using System.Collections.Generic;

namespace Scv.Api.Infrastructure.Options;

public sealed class JobsFailureEmailOptions
{
    public List<string> Recipients { get; set; } = [];
    public string Subject { get; set; } = "Background job failed";
}
