using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Scv.Core.ContractResolver;
using Scv.Core.Helpers.Extensions;
using Scv.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class FileStrategy : IDocumentStrategy
{
    private readonly FileServicesClient _filesClient;
    private readonly ClaimsPrincipal _currentUser;
    private readonly IConfiguration _configuration;

    public FileStrategy(FileServicesClient filesClient, ClaimsPrincipal currentUser, IConfiguration configuration)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        _currentUser = currentUser;
        _configuration = configuration;
    }

    public DocumentType Type => DocumentType.File;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {

        var documentResponseStreamCopy = new MemoryStream();
        var documentId = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(documentRequest.DocumentId));

        documentRequest.CorrelationId ??= Guid.NewGuid().ToString();

        using var response = await _filesClient.FilesDocumentAsync(
            _currentUser.AgencyCode(),
            _currentUser.ParticipantId(),
            _configuration.GetNonEmptyValue("Request:ApplicationCd"),
            documentId,
            documentRequest.IsCriminal ? "R" : "I",
            documentRequest.FileId,
            true,
            documentRequest.CorrelationId);

        if (response?.Stream == null)
        {
            throw new InvalidOperationException(
                $"File document response stream is null for DocumentId {documentId}, FileId {documentRequest.FileId}");
        }

        if (response.Stream.CanSeek)
        {
            response.Stream.Position = 0;
        }

        await response.Stream.CopyToAsync(documentResponseStreamCopy);
        documentResponseStreamCopy.Position = 0;

        if (documentResponseStreamCopy.Length == 0)
        {
            throw new InvalidOperationException(
                $"File document stream is empty for DocumentId {documentId}, FileId {documentRequest.FileId}");
        }

        return documentResponseStreamCopy;
    }
}
