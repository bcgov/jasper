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

        MemoryStream documentStream = null;

        try
        {
            var response = await _transcriptsClient.GetAttachmentBaseAsync(
                documentRequest.OrderId,
                documentRequest.DocumentId);

            if (response?.Stream == null)
            {
                _logger.LogError(
                    "Transcript attachment response or stream is null - OrderId: {OrderId}, DocumentId: {DocumentId}",
                    documentRequest.OrderId,
                    documentRequest.DocumentId);
                throw new InvalidOperationException(
                    $"Failed to retrieve transcript attachment: Response stream is null for OrderId {documentRequest.OrderId}, DocumentId {documentRequest.DocumentId}");
            }

            documentStream = new MemoryStream();

            // Copy the stream completely before disposing the response
            await response.Stream.CopyToAsync(documentStream);
            response.Dispose();

            documentStream.Position = 0; // Reset position for reading

            if (documentStream.Length == 0)
            {
                _logger.LogError(
                    "Transcript attachment stream is empty - OrderId: {OrderId}, DocumentId: {DocumentId}",
                    documentRequest.OrderId,
                    documentRequest.DocumentId);
                await documentStream.DisposeAsync();
                throw new InvalidOperationException(
                    $"Transcript attachment is empty for OrderId {documentRequest.OrderId}, DocumentId {documentRequest.DocumentId}");
            }

            var statusCode = response.StatusCode;
            var headers = response.Headers;

            _logger.LogInformation(
                "Transcript response - OrderId: {OrderId}, DocumentId: {DocumentId}, " +
                "Status: {Status}, ContentType: {ContentType}, ContentLength: {ContentLength}, ContentDisposition: {ContentDisposition}",
                documentRequest.OrderId,
                documentRequest.DocumentId,
                statusCode,
                headers.TryGetValue("Content-Type", out var ct) ? string.Join(",", ct) : "(none)",
                headers.TryGetValue("Content-Length", out var cl) ? string.Join(",", cl) : "(none)",
                headers.TryGetValue("Content-Disposition", out var cd) ? string.Join(",", cd) : "(none)");

            return documentStream;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Error fetching transcript attachment - OrderId: {OrderId}, DocumentId: {DocumentId}",
                documentRequest.OrderId,
                documentRequest.DocumentId);
            throw;
        }
    }
}
