namespace Scv.Core.Helpers;

/// <summary>
/// Provides utility methods for sanitizing string values.
/// </summary>
public static class StringSanitizer
{
    /// <summary>
    /// Sanitizes a string by removing carriage returns, line feeds, and trimming whitespace.
    /// </summary>
    /// <param name="value">The string value to sanitize.</param>
    /// <returns>The sanitized string, or null if the input is null.</returns>
    public static string? Sanitize(string? value)
    {
        if (value == null)
        {
            return null;
        }

        return value
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();
    }
}
