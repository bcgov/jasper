using Scv.Models.Document;
using System.Threading.Tasks;

namespace Scv.Api.Documents;

public interface IDocumentMerger
{
    /// <summary>
    /// Merges multiple PDF documents into a single PDF document in base64 format.
    /// </summary>
    /// <param name="documentRequests">An array of document requests to merge documents from</param>
    /// <returns>The merge result</returns>
    Task<PdfDocumentResponse> MergeDocuments(PdfDocumentRequest[] documentRequests);
}