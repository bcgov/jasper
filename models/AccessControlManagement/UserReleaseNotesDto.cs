using System;

namespace Scv.Models.AccessControlManagement
{
    public class UserReleaseNotesDto
    {
        public string? LastViewedVersion { get; set; }
        public DateTime? LastViewedAt { get; set; }
    }
}