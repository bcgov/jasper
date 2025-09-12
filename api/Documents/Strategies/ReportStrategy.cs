using System.IO;
using System.Threading.Tasks;
using PCSSCommon.Clients.ReportServices;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class ReportStrategy(ReportServicesClient reportServiceClient) : IDocumentStrategy
{
    private readonly ReportServicesClient _reportServiceClient = reportServiceClient;

    public DocumentType Type => DocumentType.Report;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        (Stream pdfStream, _) = await _reportServiceClient.GetCourtListReportAsync(
                documentRequest.CourtDivisionCd,
                documentRequest.Date.GetValueOrDefault(),
                documentRequest.LocationId.GetValueOrDefault(),
                documentRequest.CourtClassCd,
                documentRequest.RoomCode,
                documentRequest.AdditionsList,
                documentRequest.ReportType);

        return (MemoryStream)pdfStream;
    }
}