using System.IO;
using System.Threading.Tasks;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents.Strategies;

public interface IDocumentStrategy
{
    public DocumentType Type { get; }
    public Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest);
}
