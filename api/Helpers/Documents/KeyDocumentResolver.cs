using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Scv.Api.Models.Criminal.Detail;
using Scv.Db.Models;

namespace Scv.Api.Helpers.Documents;

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
        var safeDocuments = documents?
            .Where(d => d != null)
            .ToList() ?? [];

        if (safeDocuments.Count == 0)
        {
            return [];
        }

        // Get all Reports
        var reportDocs = safeDocuments
            .Where(d => (d.Category?.ToUpper()) == DocumentCategory.REPORT);

        // Get other key documents (excluding PSR)
        var otherKeyDocs = safeDocuments
            .Where(d =>
            DocumentCategory.KEY_DOCUMENT_CATEGORIES.Contains(d.Category?.ToUpper()) &&
            (d.Category?.ToUpper()) != DocumentCategory.REPORT);

        // Get most recent uncancelled bail document
        var bailDoc = safeDocuments
            .Where(d =>
            (d.Category?.ToUpper() == DocumentCategory.BAIL) &&
            (d.DocmDispositionDsc == null || !d.DocmDispositionDsc.Equals(_cancelled, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescendingIssueDate()
            .FirstOrDefault();

        return reportDocs
            .Where(d => d != null)
            .Concat(otherKeyDocs)
            .Concat(bailDoc != null ? [bailDoc] : Array.Empty<CriminalDocument>());
    }

    public static IOrderedEnumerable<T> OrderByDescendingIssueDate<T>(this IEnumerable<T> source) where T : CriminalDocument
    {
        return (source ?? Enumerable.Empty<T>())
            .OrderByDescending(d => DateTime.TryParse(d.IssueDate, CultureInfo.InvariantCulture, out var date) ? date : DateTime.MinValue);
    }
}