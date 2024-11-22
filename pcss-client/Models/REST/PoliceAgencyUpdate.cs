using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PCSS.Models.REST
{
    public class PoliceAgencyUpdate
    {
        public double AgenId { get; set; }
        public string AgenAgencyNm { get; set; }
        public string AgenAgencyIdentifierCd { get; set; }
    }
}
