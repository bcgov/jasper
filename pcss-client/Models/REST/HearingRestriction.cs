using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public abstract class BaseHearingRestriction
    {
        public string HearingRestrictionId { get; set; }
        public int? PcssHearingRestrictionId { get; set; }
        public string AdjPartId { get; set; }
        public string AdjFullNm { get; set; }
        public string AdjInitialsTxt { get; set; }
        public string HearingRestrictionTypeCd { get; set; }
        public string HearingRestrictionTypeDsc { get; set; }
        public string HearingRestrictionCcn { get; set; }
        public string ResponseMessageTxt { get; set; }
        public string ResponseCd { get; set; }
        public string FileNoTxt { get; set; }
    }
    
    public class CriminalHearingRestriction : BaseHearingRestriction
    {          
        public string JustinNo { get; set; }
        public string PartId { get; set; }
        public string ProfSeqNo { get; set; }
        public string PartNm { get; set; }
    }

    public class CivilHearingRestriction : BaseHearingRestriction
    {
        public string PhysicalFileId { get; set; }
        public string CivilDocumentId { get; set; }
        public string ApplyToNm { get; set; }
    }

    public class AppearanceAdjudicatorRestriction
    {
        public int? AppearanceAdjudicatorRestrictionId { get; set; }
        public int? HearingRestrictionId { get; set; }
        public string HearingRestrictionCd { get; set; }
        public int? JudgeId { get; set; }
        public string JudgesInitials { get; set; }
        public string FileNoTxt { get; set; }
        public string HearingRestrictionTxt { get; set; }
        public bool HasIssue { get; set; }
    }
}
