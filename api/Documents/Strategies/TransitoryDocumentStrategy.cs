using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Document;
using System.IO;
using System.Threading.Tasks;

namespace Scv.Api.Documents.Strategies;

public class TransitoryDocumentStrategy(ITransitoryDocumentsService transitoryDocumentsService) : IDocumentStrategy
{
    private readonly ITransitoryDocumentsService _transitoryDocumentsService = transitoryDocumentsService;

    public DocumentType Type => DocumentType.TransitoryDocument;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var documentResponseStreamCopy = new MemoryStream();

        var fileResponse = await _transitoryDocumentsService.DownloadFile(documentRequest.Path);

        await fileResponse.Stream.CopyToAsync(documentResponseStreamCopy); // follows existing pattern.
        documentResponseStreamCopy.Position = 0;

        return documentResponseStreamCopy;
    }
}
