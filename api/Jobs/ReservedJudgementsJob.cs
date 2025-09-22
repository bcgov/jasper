using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;

namespace Scv.Api.Jobs;

public class ReservedJudgementsJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<ReservedJudgementsJob> logger,
    IEmailService emailService)
    : RecurringJobBase<ReservedJudgementsJob>(configuration, cache, mapper, logger)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IEmailService _emailService = emailService;


    public override string JobName => nameof(ReservedJudgementsJob);

    public override string CronSchedule =>
        _configuration.GetValue<string>("JobSchedule:ReservedJudgementsJob") ?? base.CronSchedule;

    public override Task Execute()
    {
        var mailbox = _configuration.GetValue<string>("Azure:ServiceAccount");

        var messages = _emailService.GetFilteredEmailsAsync(
            mailbox,
            "A new version of Outstanding RJ Report for Dashboard for Email Delivery is available",
            "test@test.com");

        foreach (var message in messages.Result)
        {
            this.Logger.LogInformation("Found email with subject: {Subject}", message.Subject);
        }

        return Task.CompletedTask;
    }
}
