namespace Scv.Models.Dars
{
    public class TranscriptDocument
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public int PagesComplete { get; set; }
        public int StatusCodeId { get; set; }
        public IEnumerable<TranscriptAppearance> Appearances { get; set; }
    }

    public class TranscriptAppearance
    {
        public string AppearanceDt { get; set; }
        public string AppearanceReasonCd { get; set; }
        public string AppearanceTm { get; set; }
        public string JustinAppearanceId { get; set; }
        public string CeisAppearanceId { get; set; }
        public string CourtAgencyId { get; set; }
        public string CourtRoomCd { get; set; }
        public string JudgeFullNm { get; set; }
        public int EstimatedDuration { get; set; }
        public string EstimatedStartTime { get; set; }
        public int FileId { get; set; }
        public int Id { get; set; }
        public bool IsInCamera { get; set; }
        public int StatusCodeId { get; set; }
    }
}
