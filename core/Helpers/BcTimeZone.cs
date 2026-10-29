namespace Scv.Core.Helpers;

public static class BcTimeZone
{
    /// <summary>
    /// BC is on permanent UTC-7 like Yukon. Don't use Pacific (still has DST).
    /// </summary>
    public static readonly TimeZoneInfo Instance = TimeZoneInfo.TryFindSystemTimeZoneById("America/Whitehorse", out var tz)
        ? tz
        : TimeZoneInfo.FindSystemTimeZoneById("Yukon Standard Time");

    /// <summary>
    /// The current date and time in BC, independent of the server's time zone.
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTime(DateTime.UtcNow, Instance);

    /// <summary>
    /// The current date in BC, independent of the server's time zone.
    /// </summary>
    public static DateTime Today => Now.Date;
}
