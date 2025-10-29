using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
