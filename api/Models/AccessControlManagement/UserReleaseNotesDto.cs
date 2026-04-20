using System;

namespace Scv.Api.Models.AccessControlManagement;

public class UserReleaseNotesDto
{
    public string? LastViewedVersion { get; set; }
    public DateTime? LastViewedAt { get; set; }
}
