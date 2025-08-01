using System;
using System.Collections.Generic;
using System.Linq;
using Scv.Api.Models.Criminal.Detail;
using Scv.Db.Models;

namespace Scv.Api.Helpers.Documents;

/// <summary>
/// Provides methods to resolve key criminal documents based on specific categories and dispositions.
/// </summary>
public static class KeyDocumentResolver
{
    private static readonly string _perfected = "PERFECTED";
    private static readonly string _bail = "BAIL";

    /// <summary>
    /// Retrieves key criminal documents from the provided collection based on predefined categories and perfected bail documents.
    /// </summary>
    /// <param name="documents">An enumerable collection of <see cref="CriminalDocument"/> objects to filter.</param>
    /// <returns>
    /// An <see cref="IEnumerable{CriminalDocument}"/> containing documents that match the key categories ("ROP", "INITIATING")
    /// and the most recent perfected bail document, if available. Returns <c>default</c> if the input collection is empty.
    /// </returns>
    public static IEnumerable<CriminalDocument> GetCriminalKeyDocuments(IEnumerable<CriminalDocument> documents)
    {
        if (!documents.Any())
        {
            return [];
        }
        var nonBails = documents.Where(dmt => DocumentCategory.KEY_DOCUMENT_CATEGORIES.Contains(dmt.Category?.ToUpper() ?? dmt.DocmClassification?.ToUpper()));
        var bails = documents
            .OrderBy(dmt =>
            {
                return DateTime.TryParse(dmt.IssueDate, out DateTime date) ? date : DateTime.MinValue;
            })
            // We want the most recent perfected bail document to be included in the key documents.
            .FirstOrDefault(dmt =>
                (
                    ((dmt.Category?.ToUpper() == _bail) || (dmt.DocmClassification?.ToUpper() == _bail)) &&
                    dmt.DocmDispositionDsc.Equals(_perfected, StringComparison.OrdinalIgnoreCase)
                )
            );
        return nonBails.Concat(bails is not null ? [bails] : []);
    }
}