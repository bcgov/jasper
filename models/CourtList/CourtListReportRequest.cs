namespace Scv.Models.CourtList
{
    public class CourtListReportRequest
    {
        public string CourtDivision { get; set; }
        public DateTime? Date { get; set; }
        public int? LocationId { get; set; }
        public string CourtClass { get; set; }
        public string RoomCode { get; set; }
        public string AdditionsList { get; set; }
        public string ReportType { get; set; }
    }
}
