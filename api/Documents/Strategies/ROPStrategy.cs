using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class ROPStrategy(FileServicesClient filesClient, ClaimsPrincipal currentUser) : IDocumentStrategy
{
    private readonly FileServicesClient _filesClient = filesClient;
    private readonly ClaimsPrincipal _currentUser = currentUser;

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