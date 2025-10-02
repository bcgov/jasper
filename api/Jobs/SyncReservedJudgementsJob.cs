using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnsClient.Internal;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Documents.Parsers;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models;
using Scv.Api.Models.Search;
using Scv.Api.Services;
using Scv.Api.Services.Files;
using Scv.Db.Models;
using static PCSSCommon.Models.ActivityClassUsage;

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
    LocationService locationService)
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

    public override string JobName => nameof(SyncReservedJudgementsJob);

    public override string CronSchedule =>
        this.Configuration.GetValue<string>("JOBS:SYNC_RESERVED_JUDGEMENTS_SCHEDULE") ?? base.CronSchedule;

    public override async Task Execute()
    {
        try
        {
            this.Logger.LogInformation("Starting to process today's reserved judgements.");

            var newRJs = await GetNewReservedJudgements();
            if (newRJs.Length == 0)
            {
                this.Logger.LogInformation("No RJs have been processed");
                return;
            }

            var existingRJs = await _rjService.GetAllAsync();
            await _rjService.DeleteRangeAsync([.. existingRJs.Select(rj => rj.Id)]);

            var newRJsDtos = this.Mapper.Map<List<ReservedJudgementDto>>(newRJs.Where(rj => rj.AppearanceId != null));
            await _rjService.AddRangeAsync(newRJsDtos);

            this.Logger.LogInformation("Received {AllRJsCount} RJs. Successfully processed {ValidRJsCount}.", newRJs.Length, newRJsDtos.Count);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error occurred while processing today's reserved judgements.", ex);
        }
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
        var newRJsTask = parsedRJs.Select(crj => PopulateMissingInfo(crj));
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

    private async Task<ReservedJudgement> PopulateMissingInfo(CsvReservedJudgement crj)
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

        // Use Court File Search endpoint to determine the next appearance date.
        rj.DueDate = await PopulateNextAppearanceDate(appearance, crj.FacilityCode);

        return rj;
    }

    private async Task<string> PopulateNextAppearanceDate(PcssCounsel.ActivityAppearanceDetail appearance, string facilityCode)
    {
        var locationCode = await _locationService.GetLocationCodeByAgencyIdentifierCd(facilityCode);
        if (string.IsNullOrWhiteSpace(locationCode))
        {
            this.Logger.LogWarning("Could not find location code for facility code: {FacilityCode}", facilityCode);
            return string.Empty;
        }

        FileSearchResponse response = null;
        Enum.TryParse(appearance.CourtClassCd, ignoreCase: true, out CourtClassCd courtClassCd);

        if (string.Equals(appearance.CourtDivisionCd, CourtCalendarDetailAppearanceCourtDivisionCd.R.ToString()))
        {
            response = await _criminalFilesService.SearchAsync(new FilesCriminalQuery
            {
                CourtClass = courtClassCd,
                FileNumberTxt = appearance.CourtFileNumber,
                SearchMode = SearchMode.FILENO,
                FileHomeAgencyId = locationCode
            });

        }
        else if (string.Equals(appearance.CourtDivisionCd, CourtCalendarDetailAppearanceCourtDivisionCd.I.ToString()))
        {
            response = await _civilFilesService.SearchAsync(new FilesCivilQuery
            {
                CourtClass = courtClassCd,
                FileNumber = appearance.CourtFileNumber,
                SearchMode = SearchMode.FILENO,
                FileHomeAgencyId = locationCode
            });
        }

        if (response == null || response.FileDetail == null || response.FileDetail.Count == 0)
        {
            this.Logger.LogWarning("Could not find case details for FileNumber: {FileNumber}, CourtClass: {CourtClass}, Location: {LocationCode}",
                appearance.CourtFileNumber, courtClassCd, locationCode);
            return string.Empty;
        }

        if (response.FileDetail.Count > 1)
        {
            this.Logger.LogWarning("There should only be one case detail but search returned more than 1");
        }

        return response.FileDetail.FirstOrDefault()?.NextApprDt ?? string.Empty;
    }
}
