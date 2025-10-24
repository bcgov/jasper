using System.IO;
using System.Threading.Tasks;
using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class TransitoryDocumentStrategy(TransitoryDocumentsService transitoryDocumentsService) : IDocumentStrategy
{
    private readonly TransitoryDocumentsService _transitoryDocumentsService = transitoryDocumentsService;

    public DocumentType Type => DocumentType.TransitoryDocument;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var documentResponseStreamCopy = new MemoryStream();

        var fileResponse = await _transitoryDocumentsService.DownloadFile(
            documentRequest.BearerToken,
            documentRequest.Path);

        await fileResponse.Stream.CopyToAsync(documentResponseStreamCopy); // follows existing pattern.
        documentResponseStreamCopy.Position = 0;

        return documentResponseStreamCopy;
    }
}
