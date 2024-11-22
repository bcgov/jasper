using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class Adjudicator
    {
        public int? JudiciaryPersonId { get; set; }
        public decimal? PartID { get; set; }
        public string AdjudicatorNm { get; set; }
        public string AdjudicatorInitials { get; set; }
    }
}