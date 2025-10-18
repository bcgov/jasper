using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class ROPStrategy : IDocumentStrategy
{
    private readonly FileServicesClient _filesClient;
    private readonly ClaimsPrincipal _currentUser;

    public ROPStrategy(FileServicesClient filesClient, ClaimsPrincipal currentUser)
    {
        _filesClient = filesClient;

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
            _currentUser.ApplicationCode(),
            documentRequest.PartId,
            documentRequest.ProfSeqNo,
            courtLevelCd,
            courtClassCd);

        var bytes = Convert.FromBase64String(recordsOfProceeding.B64Content);

        return new MemoryStream(bytes);
    }
}