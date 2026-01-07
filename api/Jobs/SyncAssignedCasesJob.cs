using System.Threading;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PCSSCommon.Clients.JudicialCalendarServices;
using PCSSCommon.Clients.LookupServices;
using PCSSCommon.Models;
using Scv.Api.Documents.Parsers;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models;
using Scv.Api.Services;
using Scv.Api.Services.Files;
using Scv.Core.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Scv.Models;
using Scv.Core.Helpers.Exceptions;

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
    LookupServicesClient lookupServicesClient,
    FilesService fileService,
    ILambdaInvokerService lambdaInvokerService = null)
    : RecurringJobBase<SyncAssignedCasesJob>(configuration, cache, mapper, logger)
{
    private readonly IEmailService _emailService = emailService;
    private readonly ICsvParser _csvParser = csvParser;
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly ICaseService _caseService = caseService;
    private readonly CourtListService _courtListService = courtListService;
    private readonly JudicialCalendarServicesClient _jcServiceClient = jcServiceClient;
    private readonly LookupServicesClient _lookupServicesClient = lookupServicesClient;
    private readonly CriminalFilesService _criminalFilesService = fileService.Criminal;
    private readonly CivilFilesService _civilFilesService = fileService.Civil;
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

            // RJs
            await this.ProcessReservedJudgements();

            // Get appearance reason codes excluding CNT, DEC and ACT.
            var apprReasonCodes = await GetAppearanceReasonCodes(CaseService.ContinuationReasonCodes);

            // Get Seized scheduled CNTs, DECs and ACTs.
            await this.ProcessScheduledCases(string.Join(",", CaseService.ContinuationReasonCodes), CaseService.SEIZED_RESTRICTION_CD);

            // Get Other Seized
            await this.ProcessScheduledCases(apprReasonCodes, CaseService.SEIZED_RESTRICTION_CD);

            // Get Future Assigned - CNTs, DECs and ACTs.
            await this.ProcessScheduledCases(string.Join(",", CaseService.ContinuationReasonCodes), CaseService.ASSIGNED_RESTRICTION_CD);

            // Get Future Assigned - Others
            await this.ProcessScheduledCases(apprReasonCodes, CaseService.ASSIGNED_RESTRICTION_CD);

            this.Logger.LogInformation("SyncAssignedCasesJob completed successfully.");
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

        var validRJs = newRJs.Where(rj => rj.AppearanceId != null).ToList();
        var failedCount = newRJs.Length - validRJs.Count;

        var newRJsDtos = this.Mapper.Map<List<CaseDto>>(validRJs);
        await _caseService.AddRangeAsync(newRJsDtos);

        this.Logger.LogInformation("Received {AllRJsCount} RJs. Successfully processed {ValidRJsCount}. Failed: {FailedCount}.",
            newRJs.Length, newRJsDtos.Count, failedCount);
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

        var newRJs = await PopulateCaseInfoWithThrottling(
            parsedRJs,
            async crj =>
            {
                try
                {
                    return await PopulateMissingInfoForRJ(crj);
                }
                catch
                {
                    return this.Mapper.Map<Db.Models.Case>(crj);
                }
            },
            crj => crj.CourtFileNumber,
            "RJ");

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
        rj.RestrictionCode = CaseService.SEIZED_RESTRICTION_CD;

        // Replace the CourtClass from court list as it is what the app is accustomed to.
        rj.CourtClass = appearance.CourtClassCd;

        // No Reason and Due Date for Reserved Judgements.
        rj.Reason = null;
        rj.DueDate = null;

        return rj;
    }

    #endregion Reserved Judgements Methods

    #region Common Helper Methods

    private async Task<TOutput[]> PopulateCaseInfoWithThrottling<TInput, TOutput>(
        IEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> populateMissingInfo,
        Func<TInput, string> getItemIdentifier,
        string itemTypeName)
    {
        var itemsList = items.ToList();
        if (itemsList.Count == 0)
        {
            return [];
        }

        var maxConcurrency = this.Configuration.GetValue<int?>("JOBS:MAX_CONCURRENT_REQUESTS") ?? 10;
        var delayBetweenRequestsMs = this.Configuration.GetValue<int?>("JOBS:DELAY_BETWEEN_REQUESTS_MS") ?? 100;
        var warnings = new List<string>();
        var results = new TOutput[itemsList.Count];
        var processedCount = 0;
        var totalCount = itemsList.Count;

        this.Logger.LogInformation("Processing {Total} {ItemType}(s) with max concurrency of {MaxConcurrency} and {Delay}ms delay between requests",
            totalCount, itemTypeName, maxConcurrency, delayBetweenRequestsMs);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxConcurrency
        };

        await Parallel.ForEachAsync(
            itemsList.Select((item, index) => (item, index)),
            parallelOptions,
            async (tuple, cancellationToken) =>
            {
                var (item, index) = tuple;
                try
                {
                    results[index] = await populateMissingInfo(item);

                    // Add delay after each request to avoid overwhelming the server
                    if (delayBetweenRequestsMs > 0)
                    {
                        await Task.Delay(delayBetweenRequestsMs, cancellationToken);
                    }

                    var currentCount = Interlocked.Increment(ref processedCount);
                    if (currentCount % maxConcurrency == 0 || currentCount == totalCount)
                    {
                        this.Logger.LogInformation("Progress: {Processed}/{Total} {ItemType}(s) processed",
                            currentCount, totalCount, itemTypeName);
                    }
                }
                catch (Exception ex)
                {
                    var itemId = getItemIdentifier(item);
                    var warning = $"Failed to populate info for {itemTypeName} {itemId}: {ex.Message}";
                    lock (warnings)
                    {
                        warnings.Add(warning);
                    }
                    this.Logger.LogError(ex, "Error populating missing info for {ItemType} {ItemId}", itemTypeName, itemId);

                    // Set default value to avoid null entries in results array
                    results[index] = default;
                    Interlocked.Increment(ref processedCount);
                }
            });

        if (warnings.Count > 0)
        {
            var warningTable = string.Join("\n", warnings.Select((w, i) => $"  {i + 1,3}. {w}"));
            this.Logger.LogWarning("Encountered {Count} errors while populating {ItemType} info:\n{Warnings}",
                warnings.Count, itemTypeName, warningTable);
        }

        return [.. results.Where(r => !EqualityComparer<TOutput>.Default.Equals(r, default))];
    }

    #endregion Common Helper Methods

    #region Scheduled Cases (Decisions and Continuations) Methods

    private async Task ProcessScheduledCases(string apprReasonCodes, string restrictionCode)
    {
        this.Logger.LogInformation("Starting to retrieve scheduled cases for {ApprReasonCodes} and {RestrictionCode}", apprReasonCodes, restrictionCode);

        var scheduledCases = await this.GetScheduledCases(apprReasonCodes, restrictionCode);
        if (scheduledCases.Count == 0)
        {
            this.Logger.LogInformation(
                "No data found for {ApprReasonCodes} and {RestrictionCode}.",
                apprReasonCodes,
                restrictionCode);
            return;
        }

        var scheduledData = await PopulateCaseInfoWithThrottling(
            scheduledCases,
            async c =>
            {
                try
                {
                    return await PopulateMissingInfoForScheduledCase(c);
                }
                catch
                {
                    return PopulateScheduledCaseWithDefaults(c);
                }
            },
            c => $"{c.FileNumberTxt} (PhysicalFileId: {c.PhysicalFileId})",
            "case");

        await _caseService.AddRangeAsync([.. scheduledData
            .Select(c => {
                c.RestrictionCode = restrictionCode;
                return c;
            })
        ]);

        this.Logger.LogInformation("Processed {ScheduledCasesCount} scheduled cases for {RestrictionCode}", scheduledData.Length, restrictionCode);
    }

    private async Task<ICollection<Case>> GetScheduledCases(string apprReasonCodes, string restrictionCode)
    {
        // Retrieve the Scheduled Cases by invoking a lambda function to avoid API Gateway's timeout.
        // The endpoint is expected to take a while to get the data specially in PROD.
        var lambdaName = this.Configuration.GetValue<string>("AWS_GET_ASSIGNED_CASES_LAMBDA_NAME");
        var timeoutMinutes = this.Configuration.GetValue<int?>("AWS_GET_ASSIGNED_CASES_LAMBDA_TIMEOUT_MINUTES") ?? 10;
        if (!string.IsNullOrWhiteSpace(lambdaName))
        {
            this.Logger.LogInformation("Executing in AWS Lambda environment: {LambdaName}", lambdaName);
            var request = new
            {
                reasons = apprReasonCodes,
                restrictions = restrictionCode
            };

            var response = await _lambdaInvokerService.InvokeAsync<object, AssignedCaseResponse>(request, lambdaName, TimeSpan.FromMinutes(timeoutMinutes));

            if (response == null || !response.Success)
            {
                this.Logger.LogError("Failed to retrieve scheduled cases from Lambda: {Error}", response?.Error);
                return Array.Empty<Case>();
            }

            return response.Data;
        }
        else
        {
            return await _jcServiceClient.GetUpcomingSeizedAssignedCasesAsync(apprReasonCodes, restrictionCode);
        }
    }

    private async Task<CaseDto> PopulateMissingInfoForScheduledCase(PCSSCommon.Models.Case @case)
    {
        var caseDto = this.Mapper.Map<CaseDto>(@case);

        if (!Enum.TryParse<CourtClassCd>(caseDto.CourtClass, true, out var courtClass))
        {
            this.Logger.LogWarning("Invalid CourtClass value: {CourtClass} for PhysicalFileId: {PhysicalFileId}", caseDto.CourtClass, caseDto.PhysicalFileId);
            return caseDto;
        }

        return courtClass switch
        {
            CourtClassCd.A or CourtClassCd.Y or CourtClassCd.T
                => await PopulateCriminalCaseInfo(caseDto, @case),

            CourtClassCd.C or CourtClassCd.F or CourtClassCd.L or CourtClassCd.M
                => await PopulateCivilCaseInfo(caseDto, @case),


            _ => HandleUnsupportedCourtClass(caseDto, @case)
        };
    }

    private async Task<CaseDto> PopulateCriminalCaseInfo(CaseDto caseDto, PCSSCommon.Models.Case @case)
    {
        var criminalFile = await _criminalFilesService.AppearanceDetailAsync(caseDto.PhysicalFileId, caseDto.AppearanceId, caseDto.PartId);

        if (criminalFile == null)
        {
            this.Logger.LogWarning("Unable to find criminal file for PhysicalFileId: {PhysicalFileId}, AppearanceId: {AppearanceId} and PartId: {PartId}",
                caseDto.PhysicalFileId, caseDto.AppearanceId, caseDto.PartId);
            return PopulateScheduledCaseWithDefaults(@case);
        }

        caseDto.StyleOfCause = criminalFile.Accused?.FullNameLastFirst ?? caseDto.StyleOfCause;
        caseDto.CourtFileNumber = criminalFile.FileNumberTxt ?? caseDto.CourtFileNumber;

        return caseDto;
    }

    private async Task<CaseDto> PopulateCivilCaseInfo(CaseDto caseDto, PCSSCommon.Models.Case @case)
    {
        var civilFile = await _civilFilesService.FileIdAsync(caseDto.PhysicalFileId, false, false);

        if (civilFile == null)
        {
            this.Logger.LogWarning("Unable to find civil file for PhysicalFileId: {PhysicalFileId}, AppearanceId: {AppearanceId} and PartId: {PartId}",
                caseDto.PhysicalFileId, caseDto.AppearanceId, caseDto.PartId);
            return PopulateScheduledCaseWithDefaults(@case);
        }

        caseDto.StyleOfCause = civilFile.SocTxt ?? caseDto.StyleOfCause;
        caseDto.CourtFileNumber = civilFile.FileNumberTxt ?? caseDto.CourtFileNumber;

        return caseDto;
    }

    private CaseDto HandleUnsupportedCourtClass(CaseDto caseDto, PCSSCommon.Models.Case @case)
    {
        this.Logger.LogWarning("Unsupported CourtClass value: {CourtClass} for PhysicalFileId: {PhysicalFileId}", caseDto.CourtClass, caseDto.PhysicalFileId);
        return PopulateScheduledCaseWithDefaults(@case);
    }

    private CaseDto PopulateScheduledCaseWithDefaults(PCSSCommon.Models.Case @case)
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

    private async Task<string> GetAppearanceReasonCodes(IEnumerable<string> excludeApprCodes)
    {
        async Task<ICollection<AppearanceReason>> CriminalAppearanceReasons() => await _lookupServicesClient.GetAppearanceReasonsCriminalAsync();
        async Task<ICollection<AppearanceReason>> CivilAppearanceReasons() => await _lookupServicesClient.GetAppearanceReasonsCivilAsync();
        async Task<ICollection<AppearanceReason>> FamilyAppearanceReasons() => await _lookupServicesClient.GetAppearanceReasonsFamilyAsync();
        async Task<ICollection<AppearanceReason>> JustinAppearanceReasons() => await _lookupServicesClient.GetAppearanceReasonsJustinAsync();
        async Task<ICollection<AppearanceReason>> CeisAppearanceReasons() => await _lookupServicesClient.GetAppearanceReasonsCeisAsync();

        var criminalTask = this.Cache.GetOrAddAsync($"CriminalAppearanceReasons", CriminalAppearanceReasons);
        var civilTask = this.Cache.GetOrAddAsync($"CivilAppearanceReasons", CivilAppearanceReasons);
        var familyTask = this.Cache.GetOrAddAsync($"FamilyAppearanceReasons", FamilyAppearanceReasons);
        var justinTask = this.Cache.GetOrAddAsync($"JustinAppearanceReasons", JustinAppearanceReasons);
        var ceisTask = this.Cache.GetOrAddAsync($"CeisAppearanceReasons", CeisAppearanceReasons);

        await Task.WhenAll(criminalTask, civilTask, familyTask, justinTask, ceisTask);

        var appearanceReasons = criminalTask.Result
            .Concat(civilTask.Result)
            .Concat(familyTask.Result)
            .Concat(justinTask.Result)
            .Concat(ceisTask.Result)
            .OrderBy(a => a.Code);

        var commaDelimitedCodes = string.Join(",", appearanceReasons.Select(a => a.Code).Except(excludeApprCodes).Distinct());

        return commaDelimitedCodes;
    }

    #endregion Scheduled Cases
}
