using Mapster;
using Newtonsoft.Json;

namespace Scv.Models.Civil.Appearances
{
    /// <summary>
    /// Extends the original object.
    /// </summary>
    public class CivilAppearance : JCCommon.Clients.FileServices.CivilAppearanceDetail
    {
        public string AppearanceReasonDsc { get; set; }
        public string AppearanceResultDsc { get; set; }
        public string AppearanceStatusDsc { get; set; }
        public string CourtLocationId { get; set; }
        public string CourtLocation { get; set; }
        public string LocationId { get; set; } //PCSS - DARS location id.
        public string DocumentTypeDsc { get; set; }
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
