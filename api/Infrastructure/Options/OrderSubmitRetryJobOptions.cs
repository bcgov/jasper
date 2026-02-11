namespace Scv.Api.Infrastructure.Options;

public sealed class JobsRetrySubmitOrderOptions
{
    public string CronSchedule { get; set; } = "0 0 * * 0"; // Cron schedule for when the retry job should be run.
    public int MaxRetries { get; set; } = 9; // Limits the number of times the retry job will attempt to re-submit orders. Note that the SubmitOrderJob will retry three times when invoked by this job.
}
