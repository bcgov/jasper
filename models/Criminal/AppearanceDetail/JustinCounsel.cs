namespace Scv.Models.Criminal.AppearanceDetail
{
    public class JustinCounsel
    {
        public string? FullName => CounselGivenNm != null && CounselLastNm != null
            ? $"{CounselGivenNm.Trim()} {CounselLastNm.Trim()}"
            : null;

        public string CounselLastNm { get; set; }
        public string CounselGivenNm { get; set; }
        public string CounselEnteredDt { get; set; }
        public string CounselPartId { get; set; }
        public string CounselRelatedRepTypeCd { get; set; }
        public string CounselRrepId { get; set; }
        public string PartyAppearanceMethod { get; set; }
        public string PartyAppearanceMethodDesc { get; set; }
        public string AttendanceMethodCd { get; set; }
        public string AttendanceMethodDesc { get; set; }
        public string AppearanceMethodCd { get; set; }
        public string AppearanceMethodDesc { get; set; }
    }
}