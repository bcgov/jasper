using JCCommon.Clients.FileServices;

namespace Scv.Models.Civil.Detail
{
    /// <summary>
    /// Extends CvfcParty3.
    /// </summary>
    public class CivilParty : CvfcParty3
    {
        public string FullName => GivenNm != null && LastNm != null
            ? $"{GivenNm.Trim()} {LastNm.Trim()}"
            : OrgNm;
        public string RoleTypeDescription { get; set; }
        public string BirthDate { get; set; }
        public ICollection<ClPartyName> Aliases { get; set; }
    }
}