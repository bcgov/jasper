using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class PersonnelAvailability 
    {                    
        public string PartId { get; set; }
        public string FullNm { get; set; }
        public string AvailabilityCd { get; set; }
        public string AvailabilityDsc { get; set; }
        public int? AvailabilityWeightFactorCd { get; set; }
        public DateTime? Date { get; set; }    
        public string DateStr { get; set;}
        public string PersonTypeCd { get; set; }

        //task 2651 - extend personnel availability to include commitments/counts
        public List<PersonnelCommitment> Commitments { get; set; }

        public PersonnelAvailability()
        {
            Commitments = new List<PersonnelCommitment>();
        }

        public string PinCodeTxt { get; set; }
        public string AgencyDsc { get; set; }
        public string AgencyCd { get; set; }

        public string CCSSAvailabilityCode { get; set; }
        public string CCSSAvailabilityNoteToJCM { get; set; }

        public static string ConvertToShortDesc(string ccssAvailCode)
        {
            if (ccssAvailCode == "AVAILABLE_ALL") return "A";
            if (ccssAvailCode == "AVAILABLE_AM") return "AA";
            if (ccssAvailCode == "AVAILABLE_PM") return "AP";
            if (ccssAvailCode == "LIMITED") return "LA";
            return "NA";
        }
    }

    public class PersonnelCommitment : Commitment {
        public string CommitmentCount { get; set; }
    }
}