using JCCommon.Clients.FileServices;
using Newtonsoft.Json.Serialization;
using Scv.Core.Helpers.ContractResolver;
using Scv.Core.Helpers.Extensions;
using Scv.Models;
using Scv.Models.Document;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Scv.Api.Helpers;

namespace Scv.Api.Documents.Strategies;

public class ROPStrategy : IDocumentStrategy
{
    private readonly FileServicesClient _filesClient;
    private readonly ClaimsPrincipal _currentUser;
    private readonly IConfiguration _configuration;

    public ROPStrategy(FileServicesClient filesClient, ClaimsPrincipal currentUser, IConfiguration configuration)
    {
        _filesClient = filesClient;
        _configuration = configuration;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        _currentUser = currentUser;
    }

    public DocumentType Type => DocumentType.ROP;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var courtLevelCd = Enum.Parse<JCCommon.Clients.FileServices.CourtLevelCd>(documentRequest.CourtLevelCd, true);
        var courtClassCd = Enum.Parse<JCCommon.Clients.FileServices.CourtClassCd>(documentRequest.CourtClassCd, true);
        var recordsOfProceeding = await _filesClient.FilesRecordOfProceedingsAsync(
            _currentUser.AgencyCode(),
            _currentUser.ParticipantId(),
            _configuration.GetNonEmptyValue("Request:ApplicationCd"),
            documentRequest.PartId,
            documentRequest.ProfSeqNo,
            courtLevelCd,
            courtClassCd);

        var bytes = Convert.FromBase64String(recordsOfProceeding.B64Content);

        return new MemoryStream(bytes);
    }
}