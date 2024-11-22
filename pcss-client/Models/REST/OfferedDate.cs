using PCSS.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class OfferedDate
    {
        public string DeclineRoleCd { get; set; }
        public DateTime OfferedDt { get; set; }
        public string DeclineReasonTxt { get; set; }
        public int? JustinNo { get; set; }
        public double? PhysicalFileId { get; set; }


    }
}