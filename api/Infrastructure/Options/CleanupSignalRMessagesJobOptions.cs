namespace Scv.Api.Infrastructure.Options;

public sealed class JobsCleanupSignalRMessagesOptions
{
    public string CronSchedule { get; set; } = "0 0 * * *"; // Every day at midnight
}
