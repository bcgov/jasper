using System;

namespace Scv.Db.Models;

#nullable enable
public class UserReleaseNotes
{
    public string? LastViewedVersion { get; set; }
    public DateTime? LastViewedAt { get; set; }
}
