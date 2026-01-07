using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;
using System.Collections.Generic;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.DOCUMENT_CATEGORIES)]
public class DocumentCategory : EntityBase
{
    public const string AFFIDAVITS = "AFFIDAVITS";
    public const string BAIL = "BAIL";
    public const string INITIATING = "INITIATING";
    public const string MOTIONS = "MOTIONS";
    public const string ORDERS = "ORDERS";
    public const string PLEADINGS = "PLEADINGS";
    public const string PSR = "PSR";
    public const string REPORT = "REPORT";
    public const string ROP = "ROP";
    public const string CSR = "CSR";
    public const string LITIGANT = "LITIGANT";

    public static readonly List<string> ALL_DOCUMENT_CATEGORIES = [
        AFFIDAVITS,
        BAIL,
        INITIATING,
        MOTIONS,
        ORDERS,
        PLEADINGS,
        PSR,
        LITIGANT,
        CSR
    ];

    public static readonly List<string> KEY_DOCUMENT_CATEGORIES = [
        INITIATING,
        ROP,
        PSR,
        REPORT
    ];

    /// <summary>
    /// Formats PCSS values (like 'PSR') to JASPER-friendly categories (like 'Report').
    /// Acronyms like 'ROP' are kept as-is, while other categories are title-cased.
    /// </summary>
    public static string Format(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return string.Empty;
        }

        var upperCategory = category.ToUpper();
        
        // Special mappings for display
        if (upperCategory == PSR) return "Report";
        if (upperCategory == ROP) return "ROP";
        if (upperCategory == CSR) return "CSR";
        
        // Default: Title case (first letter uppercase, rest lowercase)
        return char.ToUpper(category[0]) + category[1..].ToLower();
    }

    public string Name { get; set; }

    public string Value { get; set; }

    public int ExternalId { get; set; }
}
