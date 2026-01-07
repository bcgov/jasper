namespace Scv.Models.Document
{
    public class PdfDocumentRequestDetails
    {
        public string PartId { get; set; }
        public string ProfSeqNo { get; set; }
        public string CourtLevelCd { get; set; }
        public string CourtClassCd { get; set; }
        public string AppearanceId { get; set; }
        public string DocumentId { get; set; }
        public string CourtDivisionCd { get; set; }
        public string FileId { get; set; }
        public bool IsCriminal { get; set; }
        public string CorrelationId { get; set; }
        public DateTime? Date { get; set; }
        public int? LocationId { get; set; }
        public string RoomCode { get; set; }
        public string AdditionsList { get; set; }
        public string ReportType { get; set; }
        public string OrderId { get; set; }
        // Transitory Documents properties
        public string? Path { get; set; }
    }
}