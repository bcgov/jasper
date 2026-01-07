using Mapster;
using Newtonsoft.Json;

namespace Scv.Models.Criminal.Appearances
{
    /// <summary>
    /// Extends the original object.
    /// </summary>
    public class CriminalAppearanceDetail : JCCommon.Clients.FileServices.CriminalAppearanceDetail
    {
        public string FullName => GivenNm != null && LastNm != null
            ? $"{GivenNm.Trim()} {LastNm.Trim()}"
            : OrgNm;

        public string AppearanceReasonDsc { get; set; }
        public string AppearanceResultDsc { get; set; }
        public string AppearanceStatusDsc { get; set; }
        public string LocationId { get; set; } // The location id from PCSS, DARS.
        public string CourtLocationId { get; set; }
        public string CourtLocation { get; set; }
        [AdaptIgnore]
        [JsonIgnore]
        public new string OutOfTownJudgeTxt { get; set; }
        [AdaptIgnore]
        [JsonIgnore]
        public new string SupplementalEquipmentTxt { get; set; }
        [AdaptIgnore]
        [JsonIgnore]
        public new string SecurityRestrictionTxt { get; set; }
    }
}