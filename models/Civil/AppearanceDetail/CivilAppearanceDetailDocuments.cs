using JCCommon.Clients.FileServices;

namespace Scv.Models.Civil.AppearanceDetail
{
    public class CivilAppearanceDetailDocuments
    {
        public string AgencyId { get; set; }
        public string AppearanceId { get; set; }
        public string FileNumberTxt { get; set; }
        public CivilFileDetailResponseCourtLevelCd CourtLevelCd { get; set; }
        public IEnumerable<CivilAppearanceDocument> Document { get; set; }
    }
}