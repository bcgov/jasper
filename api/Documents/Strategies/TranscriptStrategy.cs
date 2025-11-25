using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using DARSCommon.Clients.TranscriptsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class TranscriptStrategy : IDocumentStrategy
{
    private readonly TranscriptsServicesClient _transcriptsClient;
    private readonly ClaimsPrincipal _currentUser;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TranscriptStrategy> _logger;

    public TranscriptStrategy(
        TranscriptsServicesClient transcriptsClient,
        ClaimsPrincipal currentUser,
        IConfiguration configuration,
        ILogger<TranscriptStrategy> logger)
    {
        _transcriptsClient = transcriptsClient;
        _currentUser = currentUser;
        _configuration = configuration;
        _logger = logger;
    }

    public DocumentType Type => DocumentType.Transcript;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var documentResponseStreamCopy = new MemoryStream();

        _logger.LogInformation(
            "Fetching transcript attachment - OrderId: {OrderId}, DocumentId: {DocumentId}",
            documentRequest.OrderId,
            documentRequest.TranscriptDocumentId);

        using var response = await _transcriptsClient.GetAttachmentBaseAsync(
            documentRequest.OrderId,
            documentRequest.TranscriptDocumentId);

        await response.Stream.CopyToAsync(documentResponseStreamCopy);

        _logger.LogInformation(
            "Transcript attachment retrieved successfully - Size: {Size} bytes",
            documentResponseStreamCopy.Length);

        return documentResponseStreamCopy;
    }
}
