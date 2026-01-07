using Scv.Models.Civil.CourtList;
using Scv.Models.Criminal.CourtList;

namespace Scv.Models.CourtList
{
    public class CourtList : JCCommon.Clients.FileServices.CourtList
    {
        public CourtList()
        {
            CriminalCourtList = new List<CriminalCourtList>();
            CivilCourtList = new List<CivilCourtList>();
        }
        public new ICollection<CriminalCourtList> CriminalCourtList { get; set; }
        public new ICollection<CivilCourtList> CivilCourtList { get; set; }
    }
}
