using System.Collections.Generic;

namespace Scv.Api.Infrastructure.Options;

public sealed class JobsSubmitOrderOptions
{
    public int RetryCount { get; set; } = 2;
}
