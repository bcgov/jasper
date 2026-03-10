using System;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Scv.Db.Repositories;

namespace Scv.Api.Jobs;

public class CleanupSignalRMessagesJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<CleanupSignalRMessagesJob> logger,
    INotificationRepository notificationRepository,
    IOptions<JobsCleanupSignalRMessagesOptions> options)
    : RecurringJobBase<CleanupSignalRMessagesJob>(configuration, cache, mapper, logger)
{
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly JobsCleanupSignalRMessagesOptions _options = options.Value;

    public override string JobName => nameof(CleanupSignalRMessagesJob);

    public override string CronSchedule => _options.CronSchedule;

    public override async Task Execute()
    {
        var retentionMinutes = Configuration.GetValue<int>("SignalR:PostgresOutboxRetentionMinutes");
        if (retentionMinutes <= 0)
        {
            Logger.LogInformation(
                "SignalR cleanup skipped because retention minutes is {RetentionMinutes}.",
                retentionMinutes);
            return;
        }

        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-retentionMinutes);
        var deleted = await _notificationRepository.DeleteOlderThanAsync(cutoff);

        Logger.LogInformation(
            "SignalR cleanup deleted {DeletedCount} messages older than {Cutoff}.",
            deleted,
            cutoff);
    }
}
