namespace Scv.Api.Infrastructure.Options;

public sealed class JobsOrderReminderOptions
{
    public string CronSchedule { get; set; } = "0 0 * * *"; // Every day at midnight
    public string ProductManagerEmail { get; set; } = ""; // JASPER product manager email
}
