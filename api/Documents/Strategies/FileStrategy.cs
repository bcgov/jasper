using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class FileStrategy : IDocumentStrategy
{
    private readonly FileServicesClient _filesClient;
    private readonly ClaimsPrincipal _currentUser;

    public FileStrategy(FileServicesClient filesClient, ClaimsPrincipal currentUser)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        _currentUser = currentUser;
    }

    public DocumentType Type => DocumentType.File;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {

        var documentResponseStreamCopy = new MemoryStream();
        var documentId = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(documentRequest.DocumentId));

        documentRequest.CorrelationId ??= Guid.NewGuid().ToString();

        var response = await _filesClient.FilesDocumentAsync(
            _currentUser.AgencyCode(),
            _currentUser.ParticipantId(),
            _currentUser.ApplicationCode(),
            documentId,
            documentRequest.IsCriminal ? "R" : "I",
            documentRequest.FileId,
            true,
            documentRequest.CorrelationId);

        await response.Stream.CopyToAsync(documentResponseStreamCopy);

        return documentResponseStreamCopy;
    }
}