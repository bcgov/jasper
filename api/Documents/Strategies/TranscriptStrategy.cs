using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using DARSCommon.Clients.TranscriptsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Models.Document;
using Scv.Models;

namespace Scv.Api.Documents.Strategies;

public class TranscriptStrategy : IDocumentStrategy
{
    private readonly TranscriptsServicesClient _transcriptsClient;
    private readonly ILogger<TranscriptStrategy> _logger;

    public TranscriptStrategy(
        TranscriptsServicesClient transcriptsClient,
        ClaimsPrincipal currentUser,
        IConfiguration configuration,
        ILogger<TranscriptStrategy> logger)
    {
        _transcriptsClient = transcriptsClient;
        _logger = logger;
    }

    public DocumentType Type => DocumentType.Transcript;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        ArgumentNullException.ThrowIfNull(documentRequest.OrderId);
        ArgumentNullException.ThrowIfNull(documentRequest.DocumentId);

        _logger.LogInformation(
            "Fetching transcript attachment - OrderId: {OrderId}, DocumentId: {DocumentId}",
            documentRequest.OrderId,
            documentRequest.DocumentId);

        using var response = await _transcriptsClient.GetAttachmentBaseAsync(
            documentRequest.OrderId,
            documentRequest.DocumentId);

        var documentStream = new MemoryStream();
        await response.Stream.CopyToAsync(documentStream);
        documentStream.Position = 0; // Reset position for reading

        _logger.LogInformation(
            "Transcript attachment retrieved successfully - Size: {Size} bytes",
            documentStream.Length);

        return documentStream;
    }
}
