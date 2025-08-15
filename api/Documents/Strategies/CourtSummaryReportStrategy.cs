using System;
using System.IO;
using System.Threading.Tasks;
using Scv.Api.Constants;
using Scv.Api.Models.Document;
using Scv.Api.Services.Files;

namespace Scv.Api.Documents.Strategies;

public class CourtSummaryReportStrategy(FilesService filesService) : IDocumentStrategy
{
    private readonly CivilFilesService _civilFilesService = filesService.Civil;

    public DocumentType Type => DocumentType.CourtSummary;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var documentResponse = await _civilFilesService.CourtSummaryReportAsync(documentRequest.AppearanceId, JustinReportName.CEISR035);
        var result = new MemoryStream(Convert.FromBase64String(documentResponse.ReportContent));

        return result;
    }
}