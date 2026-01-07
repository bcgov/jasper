using JCCommon.Clients.FileServices;
using Scv.Models.Civil.CourtList;

namespace Scv.Models.Civil.AppearanceDetail
{
    public class CivilAppearanceDetailParty
    {
        public string FullName => GivenNm != null && LastNm != null
            ? $"{GivenNm.Trim()} {LastNm.Trim()}"
            : OrgNm;
        public string PartyId { get; set; }
        public string LastNm { get; set; }
        public string GivenNm { get; set; }
        public string OrgNm { get; set; }
        public string CourtParticipantId { get; set; }
        public string PartyAppearanceMethod { get; set; }
        public string PartyAppearanceMethodDesc { get; set; }
        //These fields are from appearanceMethods.
        public string AppearanceMethodCd { get; set; }
        public string AppearanceMethodDesc { get; set; }
        //The fields below this are from CourtList. 
        public string AttendanceMethodCd { get; set; }
        public string AttendanceMethodDesc { get; set; }
        ///These collections are from <see cref="ClParty"/>
        public ICollection<CivilCounsel> Counsel { get; set; }
        /// <summary>
        /// Extended.
        /// </summary>
        public ICollection<CivilRepresentative> Representative { get; set; }
        public ICollection<ClLegalRepresentative> LegalRepresentative { get; set; }
        public IEnumerable<ClPartyRole> PartyRole { get; set; }
    }
}
