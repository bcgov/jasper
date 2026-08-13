namespace Scv.Core.Helpers.Extensions;

/// <summary>
/// Helper methods for logging. 
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Removes carriage return and line feed characters so string values cannot forge additional log entries.
    /// </summary>
    public static string SanitizeForLog(this string value) =>
        value is null ? string.Empty : value.Replace("\r", string.Empty).Replace("\n", string.Empty);

    /// <summary>
    /// Masks an email for safe logging.
    /// </summary>
    /// <param name="value">The email address to mask for logging.</param>
    /// <returns>The masked email address, or "***" if the input is null, whitespace, or not a valid email shape.</returns>
    public static string MaskEmailForLog(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "***";
        }

        var sanitized = value.SanitizeForLog();
        var atIndex = sanitized.IndexOf('@');
        if (atIndex <= 0 || atIndex == sanitized.Length - 1)
        {
            return "***";
        }

        var local = sanitized[..atIndex];
        var domain = sanitized[(atIndex + 1)..];
        return $"{local[0]}***@{domain}";
    }
}
