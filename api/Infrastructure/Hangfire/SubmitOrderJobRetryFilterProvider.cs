using System.Collections.Generic;
using System.Linq;
using Hangfire;
using Hangfire.Common;
using Scv.Api.Jobs;

namespace Scv.Api.Infrastructure.Hangfire;

public sealed class SubmitOrderJobRetryFilterProvider : IJobFilterProvider
{
    private readonly int _attempts;

    public SubmitOrderJobRetryFilterProvider(int attempts)
    {
        _attempts = attempts;
    }

    public IEnumerable<JobFilter> GetFilters(Job job)
    {
        if (job?.Type != typeof(SubmitOrderJob))
        {
            return Enumerable.Empty<JobFilter>();
        }

        return new[]
        {
            new JobFilter(new AutomaticRetryAttribute { Attempts = _attempts }, JobFilterScope.Type, null)
        };
    }
}
