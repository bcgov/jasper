using System.Linq;

namespace Scv.Models;

/// <summary>
/// Represents a standardized error payload returned to the frontend.
/// </summary>
public class ValidatorErrorResponse
{
    public string Message { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();

    public static ValidatorErrorResponse FromErrors(IEnumerable<string> errors, string? message = null)
    {
        var errorList = errors?
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .ToArray() ?? Array.Empty<string>();

        var finalMessage = string.IsNullOrWhiteSpace(message)
            ? string.Join(" ", errorList).Trim()
            : message.Trim();

        if (string.IsNullOrWhiteSpace(finalMessage))
        {
            finalMessage = "An unexpected error occurred.";
        }

        return new ValidatorErrorResponse
        {
            Message = finalMessage,
            Errors = errorList
        };
    }
}
