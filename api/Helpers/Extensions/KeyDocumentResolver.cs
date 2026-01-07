using Scv.Db.Models;
using Scv.Api.Models.Criminal.Detail;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Scv.Api.Helpers;

/// <summary>
/// Provides methods to resolve key criminal documents based on specific categories and dispositions.
/// </summary>
public static class KeyDocumentResolver
{
    private static readonly string _cancelled = "CANCELLED";

    /// <summary>
    /// Retrieves key criminal documents from the provided collection based on predefined categories and uncancelled bail documents.
    /// </summary>
    /// <param name="documents">An enumerable collection of <see cref="CriminalDocument"/> objects to filter.</param>
    /// <returns>
    /// An <see cref="IEnumerable{CriminalDocument}"/> containing documents that match the key categories ("ROP", "INITIATING")
    /// and the most recent uncancelled bail document, if available. Returns <c>default</c> if the input collection is empty.
    /// </returns>
    public static IEnumerable<CriminalDocument> GetCriminalKeyDocuments(IEnumerable<CriminalDocument> documents)
    {
        if (!documents.Any())
        {
            return [];
        }

        // Get most recent Report document
        var reportDoc = documents
            .Where(d => (d.Category?.ToUpper()) == DocumentCategory.REPORT)
            .OrderByDescendingIssueDate()
            .FirstOrDefault();

        // Get other key documents (excluding PSR)
        var otherKeyDocs = documents
            .Where(d =>
            DocumentCategory.KEY_DOCUMENT_CATEGORIES.Contains(d.Category?.ToUpper()) &&
            (d.Category?.ToUpper()) != DocumentCategory.REPORT);

        // Get most recent uncancelled bail document
        var bailDoc = documents
            .Where(d =>
            (d.Category?.ToUpper() == DocumentCategory.BAIL) &&
            (d.DocmDispositionDsc == null || !d.DocmDispositionDsc.Equals(_cancelled, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescendingIssueDate()
            .FirstOrDefault();

        return new[] { reportDoc }
            .Where(d => d != null)
            .Concat(otherKeyDocs)
            .Concat(bailDoc != null ? [bailDoc] : Array.Empty<CriminalDocument>());
    }

    public static IOrderedEnumerable<T> OrderByDescendingIssueDate<T>(this IEnumerable<T> source) where T : CriminalDocument
    {
        return source.OrderByDescending(d => DateTime.TryParse(d.IssueDate, CultureInfo.InvariantCulture, out var date) ? date : DateTime.MinValue);
    }
}