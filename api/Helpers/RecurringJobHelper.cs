using System;
using System.Linq;
using Hangfire;
using Scv.Api.Jobs;

namespace Scv.Api.Helpers;

public static class RecurringJobHelper
{
    public static void AddOrUpdate(IRecurringJob job)
    {
        string[] disabled = ["disable", "disabled"];
        if (!string.IsNullOrWhiteSpace(job.CronSchedule)
            && !disabled.Any(e => e.Equals(job.CronSchedule, StringComparison.OrdinalIgnoreCase)))
        {
            var options = new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Vancouver")
            };

            RecurringJob.AddOrUpdate(
                job.JobName,
                () => job.Execute(),
                job.CronSchedule,
                options);
        }
        else
        {
            // Job is disabled. Remove it.
            RecurringJob.RemoveIfExists(job.JobName);
        }
    }
}
