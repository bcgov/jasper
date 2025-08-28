using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Scv.Api.Models.Document;
using Scv.Api.Services.Files;

namespace Scv.Api.Documents.Strategies;

public class FileStrategy(FilesService filesService) : IDocumentStrategy
{
    private readonly FilesService _filesService = filesService;
    
    public DocumentType Type => DocumentType.File;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var documentResponseStreamCopy = new MemoryStream();
        var documentId = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(documentRequest.DocumentId));
        var documentResponse = await _filesService.DocumentAsync(documentId, documentRequest.IsCriminal, documentRequest.FileId, documentRequest.CorrelationId);
        await documentResponse.Stream.CopyToAsync(documentResponseStreamCopy);
        
        return documentResponseStreamCopy;
    }
}