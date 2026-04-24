using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;
using Scv.Models.Document;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.DOCUMENT_CATEGORIES)]
public class DocumentCategory : EntityBase
{
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
        if (upperCategory == DocumentCategories.PSR) return "Report";
        if (upperCategory == DocumentCategories.ROP) return "ROP";
        if (upperCategory == DocumentCategories.CSR) return "CSR";

        // Default: Title case (first letter uppercase, rest lowercase)
        return char.ToUpper(category[0]) + category[1..].ToLower();
    }

    public string Name { get; set; }

    public string Value { get; set; }

    public int ExternalId { get; set; }
}
