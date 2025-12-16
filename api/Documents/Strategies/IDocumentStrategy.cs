using Scv.Models;
using Scv.Models.Document;
using System.IO;
using System.Threading.Tasks;

namespace Scv.Api.Documents.Strategies;

public interface IDocumentStrategy
{
    public DocumentType Type { get; }
    public Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest);
}
