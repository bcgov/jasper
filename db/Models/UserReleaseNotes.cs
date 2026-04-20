using System;

namespace Scv.Db.Models;

public class UserReleaseNotes
{
    public string? LastViewedVersion { get; set; }
    public DateTime? LastViewedAt { get; set; }
}
