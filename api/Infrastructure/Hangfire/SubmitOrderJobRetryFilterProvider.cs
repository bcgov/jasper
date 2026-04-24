using System.Collections.Generic;
using Hangfire;
using Hangfire.Common;
using Scv.Api.Jobs;

namespace Scv.Api.Infrastructure.Hangfire;

public sealed class SubmitOrderJobRetryFilterProvider(int attempts) : IJobFilterProvider
{
    private readonly int _attempts = attempts;

    public IEnumerable<JobFilter> GetFilters(Job job)
    {
        if (job?.Type != typeof(SubmitOrderJob))
        {
            return [];
        }

        return
        [
            new JobFilter(new AutomaticRetryAttribute { Attempts = _attempts }, JobFilterScope.Type, null)
        ];
    }
}
