using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PCSSCommon.Clients.JudicialCalendarServices;
using PCSSCommon.Models;
using Scv.Api.Documents.Parsers;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models;
using Scv.Api.Services;

namespace Scv.Api.Jobs;

public class SyncAssignedCasesJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<SyncAssignedCasesJob> logger,
    IEmailService emailService,
    ICsvParser csvParser,
    IDashboardService dashboardService,
    ICaseService caseService,
    CourtListService courtListService,
    JudicialCalendarServicesClient jcServiceClient,
    ILambdaInvokerService lambdaInvokerService = null)
    : RecurringJobBase<SyncAssignedCasesJob>(configuration, cache, mapper, logger)
{
    private readonly IEmailService _emailService = emailService;
    private readonly ICsvParser _csvParser = csvParser;
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly ICaseService _caseService = caseService;
    private readonly CourtListService _courtListService = courtListService;
    private readonly JudicialCalendarServicesClient _jcServiceClient = jcServiceClient;
    private readonly ILambdaInvokerService _lambdaInvokerService = lambdaInvokerService;

    public override string JobName => nameof(SyncAssignedCasesJob);

    public override string CronSchedule =>
        this.Configuration.GetValue<string>("JOBS:SYNC_ASSIGNED_CASES_SCHEDULE") ?? base.CronSchedule;

    public override async Task Execute()
    {
        try
        {
            // Delete all existing cases before processing new ones.
            var existingCases = await _caseService.GetAllAsync();
            await _caseService.DeleteRangeAsync([.. existingCases.Select(rj => rj.Id)]);

            await this.ProcessReservedJudgements();
            await this.ProcessScheduledCases();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error occurred while processing today's cases.", ex);
        }
    }

    #region Reserved Judgements Methods

    private async Task ProcessReservedJudgements()
    {
        this.Logger.LogInformation("Starting to process today's reserved judgements.");

        var newRJs = await GetNewReservedJudgements();
        if (newRJs.Length == 0)
        {
            this.Logger.LogInformation("No RJs have been processed");
            return;
        }

        var newRJsDtos = this.Mapper.Map<List<CaseDto>>(newRJs.Where(rj => rj.AppearanceId != null));
        await _caseService.AddRangeAsync(newRJsDtos);

        this.Logger.LogInformation("Received {AllRJsCount} RJs. Successfully processed {ValidRJsCount}.", newRJs.Length, newRJsDtos.Count);
    }

    private async Task<Db.Models.Case[]> GetNewReservedJudgements()
    {
        var mailbox = this.Configuration.GetNonEmptyValue("AZURE:SERVICE_ACCOUNT");
        var subject = this.Configuration.GetNonEmptyValue("RESERVED_JUDGEMENTS:SUBJECT");
        var filename = this.Configuration.GetNonEmptyValue("RESERVED_JUDGEMENTS:ATTACHMENT_NAME");
        var fromEmail = this.Configuration.GetNonEmptyValue("RESERVED_JUDGEMENTS:SENDER");

        var messages = await _emailService.GetFilteredEmailsAsync(mailbox, subject, fromEmail);

        if (!messages.Any())
        {
            this.Logger.LogWarning("No email found with subject: {Subject}", subject);
            return [];
        }

        var recentMessage = messages.First();
        var attachments = await _emailService.GetAttachmentsAsStreamsAsync(mailbox, recentMessage.Id, filename);
        if (attachments.Count == 0 || !attachments.ContainsKey(filename))
        {
            this.Logger.LogWarning("No attachment found with filename: {Filename}", filename);
            return [];
        }

        this.Logger.LogInformation("Parsing the CSV file content.");
        var parsedRJs = _csvParser.Parse<CsvReservedJudgement>(attachments.First().Value);

        this.Logger.LogInformation("Populating missing info...");
        var newRJsTask = parsedRJs.Select(crj => PopulateMissingInfoForRJ(crj));
        var newRJs = await Task.WhenAll(newRJsTask);

        return newRJs;
    }

    private async Task<int?> DeriveJudgeId(string adjudicatorName)
    {
        if (string.IsNullOrWhiteSpace(adjudicatorName))
        {
            return null;
        }

        var (lastName, firstName) = adjudicatorName.SplitFullNameToFirstAndLast();

        if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
        {
            return null;
        }

        var judges = await _dashboardService.GetJudges();
        if (judges == null || !judges.Any())
        {
            return null;
        }

        var targetInitial = char.ToUpperInvariant(firstName.Trim()[0]);

        var filteredJudges = judges
            .Where(j =>
                string.Equals(j.LastName.Trim(), lastName.Trim(), StringComparison.OrdinalIgnoreCase)
                && char.ToUpperInvariant(j.FirstName.Trim()[0]) == targetInitial)
            .ToList();

        if (filteredJudges.Count > 1)
        {
            this.Logger.LogWarning("There is more than one judge who matches the adjudicator name: {FullName}", adjudicatorName);
        }

        return filteredJudges.FirstOrDefault()?.PersonId;
    }

    private async Task<Db.Models.Case> PopulateMissingInfoForRJ(CsvReservedJudgement crj)
    {
        var rj = this.Mapper.Map<Db.Models.Case>(crj);

        // JudgeId is not provided in the CSV so we need to retrieved it based on the name on best effort.
        var judgeId = await DeriveJudgeId(crj.AdjudicatorLastNameFirstName);
        if (!judgeId.HasValue)
        {
            this.Logger.LogWarning("Could not derive JudgeId for adjudicator name: {AdjudicatorName}, CourtFileNumber: {CourtFileNumber}",
                crj.AdjudicatorLastNameFirstName, crj.CourtFileNumber);
            return rj;
        }

        // Retrieve other info from court list.
        var courtList = await _courtListService.GetJudgeCourtListAppearances(judgeId.Value, crj.AppearanceDate);
        if (courtList == null || courtList.Items.Count == 0)
        {
            this.Logger.LogWarning("Could not find court list for JudgeId: {JudgeId}, AppearanceDate: {AppearanceDate}. CourtFileNumber: {CourtFileNumber}",
                judgeId.Value, crj.AppearanceDate, crj.CourtFileNumber);
            return rj;
        }

        var appearance = courtList.Items
            .SelectMany(i => i.Appearances)
            .FirstOrDefault(a => a.CourtFileNumber == crj.CourtFileNumber
                && a.AslFeederAdjudicators.Any(asl => asl.JudiciaryPersonId == judgeId));
        if (appearance == null)
        {
            this.Logger.LogWarning("Could not find appearance for JudgeId: {JudgeId}, CourtFileNumber: {CourtFileNumber}, AppearanceDate: {AppearanceDate}",
                judgeId.Value, crj.CourtFileNumber, crj.AppearanceDate);
            return rj;
        }

        rj.AppearanceId = appearance.AppearanceId;
        rj.PhysicalFileId = appearance.PhysicalFileId;
        rj.PartId = appearance.ProfPartId;
        rj.JudgeId = judgeId.Value;
        rj.StyleOfCause = appearance.StyleOfCause;

        // Replace the CourtClass from court list as it is what the app is accustomed to.
        rj.CourtClass = appearance.CourtClassCd;

        // No Reason and Due Date for Reserved Judgements.
        rj.Reason = null;
        rj.DueDate = null;

        return rj;
    }

    #endregion Reserved Judgements Methods

    #region Scheduled Cases (Decisions and Continuations) Methods

    private async Task ProcessScheduledCases()
    {
        this.Logger.LogInformation("Starting to process scheduled decisions and continuations.");

        var scheduledCases = await this.GetScheduledCases();
        if (scheduledCases.Count == 0)
        {
            this.Logger.LogInformation("No Scheduled Cases found.");
            return;
        }

        var scheduledData = scheduledCases.Select(c => PopulateMissingInfoForScheduledCase(c)).ToList();

        await _caseService.AddRangeAsync([.. scheduledData]);

        this.Logger.LogInformation("Processed {Count} Scheduled Cases", scheduledData.Count);
    }

    private async Task<ICollection<Case>> GetScheduledCases()
    {
        var apprReasonCodes = string.Join(",", CaseService.ContinuationReasonCodes);

        // Retrieve the Scheduled Cases by invoking a lambda function to avoid API Gateway's timeout.
        // The endpoint is expected to take a while to get the data specially in PROD.
        var lambdaName = this.Configuration.GetValue<string>("AWS_GET_ASSIGNED_CASES_LAMBDA_NAME");
        if (!string.IsNullOrWhiteSpace(lambdaName))
        {
            this.Logger.LogInformation("Executing in AWS Lambda environment: {LambdaName}", lambdaName);
            var request = new
            {
                reasons = apprReasonCodes,
                restrictions = string.Empty
            };

            var response = await _lambdaInvokerService.InvokeAsync<object, AssignedCaseResponse>(request, lambdaName);

            if (response == null || !response.Success)
            {
                this.Logger.LogError("Failed to retrieve scheduled cases from Lambda: {Error}", response?.Error);
                return Array.Empty<Case>();
            }

            return response.Data;
        }
        else
        {
            return await _jcServiceClient.GetUpcomingSeizedAssignedCasesAsync(apprReasonCodes);
        }
    }

    private CaseDto PopulateMissingInfoForScheduledCase(PCSSCommon.Models.Case @case)
    {
        var caseDto = this.Mapper.Map<CaseDto>(@case);

        if (!string.IsNullOrWhiteSpace(@case.StyleOfCause))
        {
            return caseDto;
        }

        if (@case.Participants.Count == 0)
        {
            this.Logger.LogWarning("No participants found for CourtFileNumber: {CourtFileNumber}, AppearanceDate: {AppearanceDate}. Style of Cause cannot be populated.",
                caseDto.CourtFileNumber, caseDto.AppearanceDate);
            return caseDto;
        }

        var firstParticipant = @case.Participants.First();
        var participantCount = @case.Participants.Count - 1;

        caseDto.StyleOfCause = participantCount > 0
            ? $"{firstParticipant.FullName} and {participantCount} other(s)"
            : firstParticipant.FullName;

        return caseDto;
    }

    #endregion Scheduled Cases (Decisions and Continuations) Methods
}
