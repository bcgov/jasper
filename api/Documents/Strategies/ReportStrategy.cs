using System.IO;
using System.Threading.Tasks;
using Scv.Api.Models.CourtList;
using Scv.Api.Models.Document;
using Scv.Api.Services;

namespace Scv.Api.Documents.Strategies;

public class ReportStrategy(CourtListService courtListService) : IDocumentStrategy
{
    private readonly CourtListService _courtListService = courtListService;
    
    public DocumentType Type => DocumentType.Report;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var courtListReportRequest = new CourtListReportRequest
        {
            CourtDivision = documentRequest.CourtDivisionCd,
            Date = documentRequest.Date,
            LocationId = documentRequest.LocationId,
            CourtClass = documentRequest.CourtClassCd,
            RoomCode = documentRequest.RoomCode,
            AdditionsList = documentRequest.AdditionsList,
            ReportType = documentRequest.ReportType
        };
        (Stream pdfStream, _) = await _courtListService.GenerateReportAsync(courtListReportRequest);

        return (MemoryStream)pdfStream;
    }
}