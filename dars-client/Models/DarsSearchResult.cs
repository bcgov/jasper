#pragma warning disable 8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace DARSCommon.Models
{
    public class DarsSearchResults
    {
        public string Date { get; set; }
        public int? LocationId { get; set; }
        public string CourtRoomCd { get; set; }
        public string Url { get; set; }
        public string FileName { get; set; }

        public string LocationNm { get; set; }
    }
}
#pragma warning restore 8618
#pragma warning restore IDE0130
