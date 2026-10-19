namespace Scv.Core.Helpers;

public static class BcTimeZone
{
    /// <summary>
    /// BC is on permanent UTC-7 like Yukon. Don't use Pacific (still has DST).
    /// </summary>
    public static readonly TimeZoneInfo Instance = TimeZoneInfo.TryFindSystemTimeZoneById("America/Whitehorse", out var tz)
        ? tz
        : TimeZoneInfo.FindSystemTimeZoneById("Yukon Standard Time");
}
