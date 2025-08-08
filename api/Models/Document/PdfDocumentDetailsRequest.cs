namespace Scv.Api.Models.Document;

public class PdfDocumentRequestDetails
{
    public string PartId { get; set; }
    public string ProfSeqNo { get; set; }
    public string CourtLevelCd { get; set; }
    public string CourtClassCd { get; set; }
    public string RequestAgencyIdentifierId { get; set; }
    public string RequestPartId { get; set; }
    public string ApplicationCd { get; set; }
    public string AppearanceId { get; set; }
    public string ReportName { get; set; }
    public string DocumentId { get; set; }
    public string CourtDivisionCd { get; set; }
    public string FileId { get; set; }
    public bool Flatten { get; set; }
    public string CorrelationId { get; set; }
}