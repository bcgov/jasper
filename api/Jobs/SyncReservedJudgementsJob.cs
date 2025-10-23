using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnsClient.Internal;
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
using Scv.Api.Services.Files;
using Scv.Db.Models;

namespace Scv.Api.Jobs;

public class SyncReservedJudgementsJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<SyncReservedJudgementsJob> logger,
    IEmailService emailService,
    ICsvParser csvParser,
    IDashboardService dashboardService,
    ICrudService<ReservedJudgementDto> rjService,
    CourtListService courtListService,
    FilesService filesService,
    LocationService locationService,
    JudicialCalendarServicesClient jcServiceClient)
    : RecurringJobBase<SyncReservedJudgementsJob>(configuration, cache, mapper, logger)
{
    private readonly IEmailService _emailService = emailService;
    private readonly ICsvParser _csvParser = csvParser;
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly ICrudService<ReservedJudgementDto> _rjService = rjService;
    private readonly CourtListService _courtListService = courtListService;
    private readonly CriminalFilesService _criminalFilesService = filesService.Criminal;
    private readonly CivilFilesService _civilFilesService = filesService.Civil;
    private readonly LocationService _locationService = locationService;
    private readonly JudicialCalendarServicesClient _jcServiceClient = jcServiceClient;

    public override string JobName => nameof(SyncReservedJudgementsJob);

    public override string CronSchedule =>
        this.Configuration.GetValue<string>("JOBS:SYNC_RESERVED_JUDGEMENTS_SCHEDULE") ?? base.CronSchedule;

    public override async Task Execute()
    {
        try
        {
            // Delete all existing RJs before processing new ones.
            var existingRJs = await _rjService.GetAllAsync();
            await _rjService.DeleteRangeAsync([.. existingRJs.Select(rj => rj.Id)]);

            await this.ProcessReservedJudgements();
            await this.ProcessScheduledCases();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error occurred while processing today's reserved judgements.", ex);
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

        var newRJsDtos = this.Mapper.Map<List<ReservedJudgementDto>>(newRJs.Where(rj => rj.AppearanceId != null));
        await _rjService.AddRangeAsync(newRJsDtos);

        this.Logger.LogInformation("Received {AllRJsCount} RJs. Successfully processed {ValidRJsCount}.", newRJs.Length, newRJsDtos.Count);
    }

    private async Task<ReservedJudgement[]> GetNewReservedJudgements()
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

    private async Task<ReservedJudgement> PopulateMissingInfoForRJ(CsvReservedJudgement crj)
    {
        var rj = this.Mapper.Map<ReservedJudgement>(crj);

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
        rj.Reason = appearance.AppearanceReasonDsc;

        // Replace the CourtClass from court list as it is what the app is accustomed to.
        rj.CourtClass = appearance.CourtClassCd;

        // No Due Date for Reserved Judgements.
        rj.DueDate = null;

        return rj;
    }

    #endregion Reserved Judgements Methods

    #region Scheduled Cases (Decisions and Continuations) Methods

    private async Task ProcessScheduledCases()
    {
        this.Logger.LogInformation("Starting to process scheduled cases.");

        var scheduledCases = await _jcServiceClient.GetUpcomingSeizedAssignedCasesAsync();
        if (scheduledCases.Count == 0)
        {
            this.Logger.LogInformation("No Scheduled Cases found.");
            return;
        }

        var scheduledDataTask = scheduledCases.Select(c => PopulateMissingInfoForScheduledCase(c));
        var newScheduledData = await Task.WhenAll(scheduledDataTask);

        await _rjService.AddRangeAsync([.. newScheduledData]);


        this.Logger.LogInformation("Processed {Count} Scheduled Cases", newScheduledData.Length);
    }

    private async Task<ReservedJudgementDto> PopulateMissingInfoForScheduledCase(Case @case)
    {
        var rj = this.Mapper.Map<ReservedJudgementDto>(@case);
        var judgeId = rj.JudgeId.GetValueOrDefault();

        if (!string.IsNullOrWhiteSpace(@case.StyleOfCause))
        {
            return rj;
        }

        this.Logger.LogInformation("Getting Style of Cause info is missing for CourtFileNumber: {CourtFileNumber}", @case.FileNumberTxt);

        // Retrieve other info from court list.
        var courtList = await _courtListService.GetJudgeCourtListAppearances(judgeId, rj.AppearanceDate);
        if (courtList == null || courtList.Items.Count == 0)
        {
            this.Logger.LogWarning("Could not find court list for JudgeId: {JudgeId}, AppearanceDate: {AppearanceDate}. CourtFileNumber: {CourtFileNumber}",
                judgeId, rj.AppearanceDate, rj.CourtFileNumber);
            return rj;
        }

        var appearance = courtList.Items
            .SelectMany(i => i.Appearances)
            .FirstOrDefault(a => a.CourtFileNumber == rj.CourtFileNumber
                && a.AslFeederAdjudicators.Any(asl => asl.JudiciaryPersonId == judgeId));
        if (appearance == null)
        {
            this.Logger.LogWarning("Could not find appearance for JudgeId: {JudgeId}, CourtFileNumber: {CourtFileNumber}, AppearanceDate: {AppearanceDate}",
                judgeId, rj.CourtFileNumber, rj.AppearanceDate);
            return rj;
        }

        rj.StyleOfCause = appearance.StyleOfCause;
        return rj;
    }

    #endregion Scheduled Cases (Decisions and Continuations) Methods
}
