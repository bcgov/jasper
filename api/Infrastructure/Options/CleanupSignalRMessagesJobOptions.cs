namespace Scv.Api.Infrastructure.Options;

public sealed class CleanupSignalRMessagesJobOptions
{
    public string CronSchedule { get; set; } = "0 0 * * *"; // Every day at midnight
}
