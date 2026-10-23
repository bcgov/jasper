using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Documents.Conversion;
using Scv.Api.Services;
using Scv.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class TransitoryDocumentStrategy(
    ITransitoryDocumentsService transitoryDocumentsService,
    IDocumentPdfConversionService documentPdfConversionService,
    ILogger<TransitoryDocumentStrategy> logger) : IDocumentStrategy
{
    private readonly ITransitoryDocumentsService _transitoryDocumentsService = transitoryDocumentsService;
    private readonly IDocumentPdfConversionService _documentPdfConversionService = documentPdfConversionService;
    private readonly ILogger<TransitoryDocumentStrategy> _logger = logger;

    public DocumentType Type => DocumentType.TransitoryDocument;

    public Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        return Invoke(documentRequest, CancellationToken.None);
    }

    public async Task<MemoryStream> Invoke(
        PdfDocumentRequestDetails documentRequest,
        CancellationToken cancellationToken)
    {
        var fileResponse = await _transitoryDocumentsService.DownloadFile(
            documentRequest.Path,
            cancellationToken);

        if (fileResponse?.Stream == null)
        {
            throw new InvalidOperationException(
                $"Transitory document response stream is null for Path {documentRequest.Path}");
        }

        await using (fileResponse.Stream)
        {
            var converted = await _documentPdfConversionService.ConvertToPdfAsync(
                new DocumentPdfConversionRequest(
                    fileResponse.Stream,
                    fileResponse.FileName,
                    documentRequest.Path),
                cancellationToken);

            _logger.LogInformation(
                "Transitory document prepared for merge - FileName: {FileName}, ContentType: {ContentType}, PdfSize: {PdfSize}",
                fileResponse.FileName,
                fileResponse.ContentType,
                converted.Length);

            return converted;
        }
    }
}
