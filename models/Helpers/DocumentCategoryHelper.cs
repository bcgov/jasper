using System.Collections.Generic;

namespace Scv.Models.Helpers;

public static class DocumentCategoryHelper
{
    public const string BAIL = "BAIL";
    public const string REPORT = "REPORT";
    public const string ROP = "ROP";
    public const string CSR = "CSR";
    public const string PSR = "PSR";
    public const string INITIATING = "INITIATING";

    public static readonly List<string> KEY_DOCUMENT_CATEGORIES =
    [
        INITIATING,
        ROP,
        PSR,
        REPORT
    ];

    public static string Format(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return string.Empty;
        }

        var upperCategory = category.ToUpperInvariant();

        if (upperCategory == PSR) return "Report";
        if (upperCategory == ROP) return "ROP";
        if (upperCategory == CSR) return "CSR";

        return char.ToUpper(category[0]) + category[1..].ToLowerInvariant();
    }
}
