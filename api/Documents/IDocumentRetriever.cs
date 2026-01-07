using Scv.Models.Document;
using System.IO;
using System.Threading.Tasks;

namespace Scv.Api.Documents;

public interface IDocumentRetriever
{
    /// <summary>
    ///  Retrieves a PDF stream from a given source based off the document-request type.
    /// </summary>
    /// <param name="documentRequest">The document request containing the source information</param>
    /// <returns>The retrieved PDF document stream</returns>
    Task<MemoryStream> Retrieve(PdfDocumentRequest documentRequest);
}