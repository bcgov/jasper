using System;
using System.Text;
using System.Text.RegularExpressions;
using AnyAscii;

namespace Scv.Api.Services;

public sealed partial class CsoTextSanitizer : ICsoTextSanitizer
{
    public string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var sanitized = text
            .Normalize(NormalizationForm.FormKC)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Transliterate();

        sanitized = HorizontalWhitespaceRegex().Replace(sanitized, " ");
        sanitized = BlankLineWhitespaceRegex().Replace(sanitized, "\n");

        return sanitized.Trim();
    }

    [GeneratedRegex("[ \\t]+")]
    private static partial Regex HorizontalWhitespaceRegex();

    [GeneratedRegex(" *\\n *")]
    private static partial Regex BlankLineWhitespaceRegex();
}
