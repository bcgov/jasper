using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Documents.Parsers;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Models.CourtLocation;

namespace Scv.Api.Jobs;

public class SyncCourtLocationsJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<SyncCourtLocationsJob> logger,
    IEmailService emailService,
    IExcelParser excelParser,
    ICourtLocationService clService)
    : RecurringJobBase<SyncCourtLocationsJob>(configuration, cache, mapper, logger)
{
    private const string COURT_LOCATIONS_SHEET = "Court Locations";
    private const string ADULT_PROBATION_OFFICES_SHEET = "Adult Probation Offices";
    private const string YOUTH_PROBATION_OFFICES_SHEET = "Youth Probation Offices";

    private readonly IEmailService _emailService = emailService;
    private readonly IExcelParser _excelParser = excelParser;
    private readonly ICourtLocationService _clService = clService;

    public override string JobName => nameof(SyncCourtLocationsJob);

    public override async Task Execute()
    {
        try
        {
            using var attachmentStream = await GetCourtLocationAttachment();
            if (attachmentStream == null)
            {
                this.Logger.LogInformation("No updated court location attachment found.");
                return;
            }

            var courtLocations = await this.GetCourtLocations(attachmentStream);

            var result = await _clService.ReplaceCourtLocationsAsync(courtLocations);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to replace court locations: {string.Join(", ", result.Errors)}");
            }

            this.Logger.LogInformation($"{nameof(SyncCourtLocationsJob)} completed successfully.");
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Unable to sync court locations.");
            throw new InvalidOperationException("Unable to sync court locations.", ex);
        }
    }

    private async Task<MemoryStream> GetCourtLocationAttachment()
    {
        var mailbox = this.Configuration.GetNonEmptyValue("AZURE:SERVICE_ACCOUNT");
        var subject = this.Configuration.GetNonEmptyValue("COURT_LOCATIONS:SUBJECT");
        var filename = this.Configuration.GetNonEmptyValue("COURT_LOCATIONS:ATTACHMENT_NAME");

        var messages = await _emailService.GetFilteredEmailsAsync(mailbox, subject, null, hasAttachment: true);


        if (!messages.Any())
        {
            this.Logger.LogWarning("No email found with subject: {Subject}", subject);
            return null;
        }

        var recentMessage = messages.First();
        var attachments = await _emailService.GetAttachmentsAsStreamsAsync(mailbox, recentMessage.Id, filename);
        if (attachments.Count == 0 || !attachments.ContainsKey(filename))
        {
            return null;
        }

        this.Logger.LogInformation("Court Location Attachment found.");
        return attachments.First().Value;
    }

    private async Task<CourtLocationDto[]> GetCourtLocations(MemoryStream stream)
    {
        using var parser = _excelParser.Open(stream);
        var parsedCourtLocations = parser.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET);
        var parsedAdultOffices = parser.GetSheet<AdultProbationOffice>(ADULT_PROBATION_OFFICES_SHEET);
        var parsedYouthOffices = parser.GetSheet<YouthProbationOffice>(YOUTH_PROBATION_OFFICES_SHEET);

        if (parsedCourtLocations.Count == 0 || parsedAdultOffices.Count == 0 || parsedYouthOffices.Count == 0)
        {
            throw new InvalidOperationException("One or more sheets are empty.");
        }

        var courtLocations = this.Mapper.Map<CourtLocationDto[]>(parsedCourtLocations);

        var adultOfficesByName = parsedAdultOffices
            .GroupBy(o => (o.Name ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => this.Mapper.Map<AdultProbationOfficeDto>(g.First()), StringComparer.OrdinalIgnoreCase);

        var youthOfficesByName = parsedYouthOffices
            .GroupBy(o => (o.Name ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => this.Mapper.Map<YouthProbationOfficeDto>(g.First()), StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < courtLocations.Length; i++)
        {
            var parsed = parsedCourtLocations[i];
            var dto = courtLocations[i];

            if (!string.IsNullOrWhiteSpace(parsed.AdultProbationOffice)
                && adultOfficesByName.TryGetValue(parsed.AdultProbationOffice.Trim(), out var adult))
            {
                dto.AdultProbationOffice = adult;
            }

            if (!string.IsNullOrWhiteSpace(parsed.YouthProbationOffice)
                && youthOfficesByName.TryGetValue(parsed.YouthProbationOffice.Trim(), out var youth))
            {
                dto.YouthProbationOffice = youth;
            }
        }

        this.Logger.LogInformation("Successfully parsed {Count} court locations.", courtLocations.Length);
        return courtLocations;
    }
}
