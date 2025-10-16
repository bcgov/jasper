using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Scv.Api.Constants;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class CourtSummaryReportStrategy(FileServicesClient filesClient, ClaimsPrincipal currentUser) : IDocumentStrategy
{
    private readonly FileServicesClient _filesClient = filesClient;
    private readonly ClaimsPrincipal _currentUser = currentUser;

    public DocumentType Type => DocumentType.CourtSummary;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var documentResponse = await _filesClient.FilesCivilCourtsummaryreportAsync(
            _currentUser.AgencyCode(),
            _currentUser.ParticipantId(),
            _currentUser.ApplicationCode(),
            documentRequest.AppearanceId,
            JustinReportName.CEISR035);

        var result = new MemoryStream(Convert.FromBase64String(documentResponse.ReportContent));

        return result;
    }
}