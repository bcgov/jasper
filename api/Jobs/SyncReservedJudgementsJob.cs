using System;
using System.Linq;
using System.Threading.Tasks;
using DnsClient.Internal;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Documents.Parsers;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Services;

namespace Scv.Api.Jobs;

public class SyncReservedJudgementsJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<SyncReservedJudgementsJob> logger,
    IEmailService emailService,
    ICsvParser csvParser,
    IDashboardService dashboardService)
    : RecurringJobBase<SyncReservedJudgementsJob>(configuration, cache, mapper, logger)
{
    private readonly IEmailService _emailService = emailService;
    private readonly ICsvParser _csvParser = csvParser;
    private readonly IDashboardService _dashboardService = dashboardService;

    public override string JobName => nameof(SyncReservedJudgementsJob);

    public override string CronSchedule =>
        this.Configuration.GetValue<string>("JOBS:SYNC_RESERVED_JUDGEMENTS_SCHEDULE") ?? base.CronSchedule;

    public override async Task Execute()
    {
        try
        {
            this.Logger.LogInformation("Starting to download today's reserved judgements.");

            // Fetch and download the email
            var mailbox = this.Configuration.GetNonEmptyValue("AZURE:SERVICE_ACCOUNT");
            var subject = this.Configuration.GetNonEmptyValue("RESERVED_JUDGEMENTS:SUBJECT");
            var filename = this.Configuration.GetNonEmptyValue("RESERVED_JUDGEMENTS:FILENAME");
            var fromEmail = this.Configuration.GetNonEmptyValue("RESERVED_JUDGEMENTS:SENDER");

            var messages = await _emailService.GetFilteredEmailsAsync(mailbox, subject, fromEmail);

            if (!messages.Any())
            {
                this.Logger.LogWarning("No email found with subject: {Subject}", subject);
                return;
            }

            var recentMessage = messages.FirstOrDefault();
            var attachments = await _emailService.GetAttachmentsAsStreamsAsync(mailbox, recentMessage.Id);
            if (attachments.Count == 0 || !attachments.ContainsKey(filename))
            {
                this.Logger.LogWarning("No attachment found with filename: {Filename}", filename);
                return;
            }

            this.Logger.LogInformation("Parsing the CSV file content.");
            var parsedRJs = _csvParser.Parse<ReservedJudgement>(attachments.FirstOrDefault().Value);

            // Get judge info from the db and map to the RJs


            this.Logger.LogInformation("Saving the new reserved judgements in the db.");
            // Delete existing RJs in the database

            // Add new RJs to the database

            this.Logger.LogInformation("Successfuly processed today's reserved judgements.");

        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error occured while downloading the today's reserved judgements.");
            throw;
        }
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

        var judge = judges.FirstOrDefault(j =>
            string.Equals(j.LastName.Trim(), lastName.Trim(), StringComparison.OrdinalIgnoreCase)
            && char.ToUpperInvariant(j.FirstName.Trim()[0]) == targetInitial);

        return judge?.UserId;
    }
}
