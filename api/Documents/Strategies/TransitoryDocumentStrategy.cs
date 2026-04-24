using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class TransitoryDocumentStrategy(
    ITransitoryDocumentsService transitoryDocumentsService,
    ILogger<TransitoryDocumentStrategy> logger) : IDocumentStrategy
{
    private readonly ITransitoryDocumentsService _transitoryDocumentsService = transitoryDocumentsService;
    private readonly ILogger<TransitoryDocumentStrategy> _logger = logger;

    public DocumentType Type => DocumentType.TransitoryDocument;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var documentResponseStreamCopy = new MemoryStream();

        var fileResponse = await _transitoryDocumentsService.DownloadFile(documentRequest.Path);

        await fileResponse.Stream.CopyToAsync(documentResponseStreamCopy); // follows existing pattern.
        documentResponseStreamCopy.Position = 0;

        var looksLikePdf = HasPdfSignature(documentResponseStreamCopy);
        var headerHex = ReadHeaderHex(documentResponseStreamCopy);

        _logger.LogInformation(
            "Transitory stream copied for merge - Path: {Path}, FileName: {FileName}, ContentType: {ContentType}, Size: {Size}, LooksLikePdf: {LooksLikePdf}, HeaderHex: {HeaderHex}",
            documentRequest.Path,
            fileResponse.FileName,
            fileResponse.ContentType,
            documentResponseStreamCopy.Length,
            looksLikePdf,
            headerHex);

        if (!looksLikePdf)
        {
            _logger.LogWarning(
                "Transitory stream does not have PDF signature - Path: {Path}, FileName: {FileName}, ContentType: {ContentType}, HeaderHex: {HeaderHex}",
                documentRequest.Path,
                fileResponse.FileName,
                fileResponse.ContentType,
                headerHex);
        }

        return documentResponseStreamCopy;
    }

    private static bool HasPdfSignature(MemoryStream stream)
    {
        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            if (stream.Length < 5)
            {
                return false;
            }

            Span<byte> prefix = stackalloc byte[5];
            var bytesRead = stream.Read(prefix);
            return bytesRead == 5
                && prefix[0] == '%'
                && prefix[1] == 'P'
                && prefix[2] == 'D'
                && prefix[3] == 'F'
                && prefix[4] == '-';
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static string ReadHeaderHex(MemoryStream stream, int maxBytes = 16)
    {
        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            var buffer = new byte[Math.Min(maxBytes, checked((int)stream.Length))];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            return bytesRead == 0 ? string.Empty : Convert.ToHexString(buffer.AsSpan(0, bytesRead));
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}
