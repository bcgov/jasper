namespace Scv.Core.Helpers.Extensions;

/// <summary>
/// Helpers for safely writing string values such as ids, names, email, etc. to logs.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Removes carriage return and line feed characters so string values cannot forge additional log entries.
    /// </summary>
    public static string SanitizeForLog(this string value) =>
        value is null ? string.Empty : value.Replace("\r", string.Empty).Replace("\n", string.Empty);
}
