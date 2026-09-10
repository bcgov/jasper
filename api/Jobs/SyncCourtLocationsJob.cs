using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;

namespace Scv.Api.Jobs;

public class SyncCourtLocationsJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<SyncCourtLocationsJob> logger,
    IEmailService emailService)
    : RecurringJobBase<SyncCourtLocationsJob>(configuration, cache, mapper, logger)
{
    private readonly IEmailService _emailService = emailService;

    public override string JobName => nameof(SyncCourtLocationsJob);

    public override Task Execute()
    {

        return Task.CompletedTask;
    }

    private async Task<CourtLocation> GetCourtLocations()
    {
        var mailbox = this.Configuration.GetNonEmptyValue("AZURE:SERVICE_ACCOUNT");
        var subject = this.Configuration.GetNonEmptyValue("COURT_LOCATIONS:SUBJECT");
        var filename = this.Configuration.GetNonEmptyValue("COURT_LOCATIONS:ATTACHMENT_NAME");

        var courtLocationEmail = await _emailService.GetFilteredEmailsAsync(mailbox, subject, filename, hasAttachment: true);
    }
}
