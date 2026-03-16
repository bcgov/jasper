namespace Scv.Api.Infrastructure.Options;

public sealed class JobsRetryUrgentSubmitOrderOptions
{
    public string CronSchedule { get; set; } = "0 0,8,16 * * *"; // Three times a day
    public int MaxRetries { get; set; } = 9; // Limits the number of urgent retry attempts.
    // This "URG" value is temporary for now until we gather more information on priority types
    public string PriorityType { get; set; } = "URG"; // Priority code for urgent orders.
}
