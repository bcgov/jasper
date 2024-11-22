using PCSS.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class Accused
    {
        public double? ProfSeqNo { get; set; }
        public double? PartId { get; set; }

        public string LastNm { get; set; }

        public string GivenNm { get; set; }

        public string OrgNm { get; set; }

        public string InCustodyYn { get; set; }

        public string WarrantYn { get; set; }

        public string BirthDt { get; set; }

        public object Charge { get; set; }
    }

    public class CivilDocument
    {
        public string CivilDocumentId { get; set; }

        public string FileSeqNo { get; set; }

        public string DocumentTypeCd { get; set; }

        public string DocumentTypeDsc { get; set; }

        public string FiledDt { get; set; }

        public string ConcludedYn { get; set; }

        public bool Selected { get; set; }
    }

    public class CivilParty
    {
        public double? PartyId { get; set; }

        public string LastNm { get; set; }

        public string GivenNm { get; set; }

        public string OrgNm { get; set; }

        public string RoleTypeCd { get; set; }

        public string RoleTypeDesc { get; set; }

        public bool Selected { get; set; }
    }    

    public abstract class AbstractScheduleEvent
    {                
        //Used in most cases except for TBA special processing when we need to create the activity on the fly
        public int CourtActivitySlotId { get; set; }

        //Only used when the slot is -1 and looking at TBA
        public string ActivityCd { get; set; }
        public string AppearanceDt { get; set; }
        public int LocationId { get; set; }
        
        //Rest is used as appropriate
        public double EstimatedQty { get; set; }
        public string EstimatedUnitCd { get; set; }        
        public string AppearanceTm { get; set; }
        public string AppearanceReasonCd { get; set; }

        public string ActivityClassCd { get; set; }

        public string ScheduleNoteTxt { get; set; }        

        public string TentativeYn { get; set; }
        public string TentativeExpiryDt { get; set; }

        public string SupplementalEquipmentTxt { get; set; }
        public string SecurityRestrictionTxt { get; set; }
        public string OutOfTownJudgeTxt { get; set; }

        public string BulkLoadFlag { get; set; }        

        public dynamic EditAppearanceSupportingData { get; set; }        

        //Logging features
        public string SearchedForDate { get; set; }
        public double? WitnessScore { get; set; }
        public double? CapacityScore { get; set; }

        public string FileNumberTxt { get; set; }

        public virtual List<string> Validate()
        {
            var errors =  new List<string>();
            DateTime dt;

            if (CourtActivitySlotId == 0)
            {
                errors.Add("Court Activity Slot cannot be 0");
            }            
            if (String.IsNullOrEmpty(AppearanceReasonCd))
            {
                errors.Add("AppearanceReasonCd cannot be null or empty");
            }
            if (String.IsNullOrEmpty(TentativeYn) || !(Constants.Y_N.Contains(TentativeYn)))
            {
                errors.Add("TentativeYn must be Y or N");
            }
            if (!DateTime.TryParseExact(AppearanceTm, Constants.TIME_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                errors.Add("AppearanceTm must be in the format " + Constants.TIME_FORMAT);
            }
            if (TentativeYn == "Y" && !DateTime.TryParseExact(TentativeExpiryDt, Constants.DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                errors.Add("TentativeExpiryDt must be in the format " + Constants.DATE_FORMAT);
            }
            if (String.IsNullOrEmpty(ActivityClassCd) || !(new string[] { "R", "I", "F" }.Contains(ActivityClassCd)))
            {
                errors.Add("ActivityClassCd must be R, I, or F");
            }

            return errors;
        }
    }
    
    /// <summary>
    /// Rest api model for setting the date for a criminal file
    /// </summary>
    public class CriminalScheduleEvent: AbstractScheduleEvent
    {        
        public int? JustinNo { get; set; }
        public IList<Accused> Accused { get; set; }

        public string BulkLoadJustinApprID { get; set; }
        public List<CriminalHearingRestriction> HearingRestrictions { get; set; }

        public override List<string> Validate()
        {
            var errors = base.Validate();

            if (JustinNo == null)
            {
                errors.Add("JustinNo is required");
            }
            
            if (TentativeYn == "N")
            {
                if (Accused == null || !Accused.Any())
                {
                    errors.Add("Accused is required");
                }
                else if (Accused.Any(x => x.PartId == null || x.ProfSeqNo == null))
                {
                    errors.Add("PartId and ProfSeqNo is required for every accused");
                }
            }
            return errors;
        }
    }

    /// <summary>
    /// Rest api model for setting the date for a criminal file
    /// </summary>
    public class CivilScheduleEvent : AbstractScheduleEvent
    {
        public string PcssCourtDivisionCd { get; set; } //Bug 2562 - ui is setting this to the proper class cd, so let's accept it and use it.
        
        public double? PhysicalFileId { get; set; }
        public IList<CivilDocument> Documents { get; set; }
        public IList<CivilParty> Parties { get; set; }
        public List<CivilHearingRestriction> HearingRestrictions { get; set; }
        public string BulkLoadCeisApprID { get; set; }

        public override List<string> Validate()
        {
            var errors = base.Validate();

            if (PhysicalFileId == null)
            {
                errors.Add("PhysicalFileId is required");
            }
            
            if (TentativeYn == "N")
            {
                if (Documents == null || !Documents.Any())
                {
                    errors.Add("Documents is required");
                }
                else if (Documents.Any(x => String.IsNullOrEmpty(x.CivilDocumentId)))
                {
                    errors.Add("CivilDocumentId is required for every document");
                }

                if (Parties == null || !Parties.Any())
                {
                    errors.Add("Parties is required");
                }
                else if (Parties.Any(x => x.PartyId == null))
                {
                    errors.Add("PartyId is required for every party");
                }
            }
            return errors;

            
        }
    }

    public class SchedulingEvents
    {
        //Only used for TBA activities
        public string ActivityCd { get; set; }
        public int LocationId { get; set; }

        public List<CriminalScheduleEvent> criminalFiles { get; set; }
        public List<CivilScheduleEvent> civilFiles { get; set; }

        public List<string> Validate()
        {
            var errorList = new List<string>();

            if (criminalFiles == null && civilFiles == null)
            {
                errorList.Add("Both the criminal and civil file list is null");
                //Stop here
                return errorList;
            }

            if ((criminalFiles != null && !criminalFiles.Any())
                && (civilFiles != null && !civilFiles.Any()))
            {
                errorList.Add("Both the criminal and civil file list is empty");
                //Stop here
                return errorList;
            }

            if ((criminalFiles != null && criminalFiles.Any())
                && (civilFiles != null && civilFiles.Any()))
            {
                errorList.Add("Both the criminal and civil file list have items.  Only one is supported at a time");
                //Stop here
                return errorList;
            }

            if (criminalFiles != null)
            {
                foreach (var file in criminalFiles) 
                {
                    errorList.AddRange(file.Validate());
                }
            }

            if (civilFiles != null)
            {
                foreach (var file in civilFiles)
                {
                    errorList.AddRange(file.Validate());
                }
            }

            return errorList;
        }        
    }

    public class AssignmentListScheduling
    {       
        public int? CourtActivityId { get; set; }
        public string CourtRoomCd  { get; set; }
        public int PcssAppearanceId { get; set; }
        public string TrialTrackerCd { get; set; }
        public double? EstimatedQty { get; set; }
        public string EstimatedUnitCd { get; set; }
        public string AppearanceReasonCd { get; set; }
        public string JcmComments { get; set; }
    }

    public class AssignmentListSchedulingEmail
    {
        public int CourtActivityId { get; set; }
        public string CourtRoomCd { get; set; }
        public string JcmComments { get; set; }
        public string AppearanceReasonCd { get; set; }
        public string AdjToPCJYn { get; set; }
        public List<ActivityAppearanceDetail> Appearances { get; set; }
    }

    public class JustinCeisSchedulingResponseDetail
    {
        public string PcssAppearanceId { get; set; }
        public string AppearanceId { get; set; }
        public string AppearanceCcn { get; set; }
        public int? LocationId { get; set; }
        public string JustinNo { get; set; }
        public string PhysicalFileId { get; set; }
        public string ProfSeqNo { get; set; }
        public string ProfPartId { get; set; }
        public string ActivityClassCd { get; set; }
    }

    public class JustinCeisSchedulingResponse
    {        
        public string ResponseCd { get; set; }
        public string ResponseMessageTxt { get; set; }
        public List<JustinCeisSchedulingResponseDetail> Details { get; set; }
        public override string ToString()
        {
            var detailsStr = "";
            if (Details != null)
            {
                foreach (var detail in Details)
                {
                    if (!string.IsNullOrWhiteSpace(detailsStr)) {
                        detailsStr = detailsStr + ",";
                    }
                    detailsStr = detailsStr + string.Format(" PCSS AppearanceId = {0}, Justin/Ceis Id = {1} ", detail.PcssAppearanceId, detail.AppearanceId);
                }
            }
            return string.Format("JustinCeisSchedulingResponse - {0} - {1} [{2}]", ResponseCd, ResponseMessageTxt, detailsStr);
        }
    }

    public class AlternativeCourtRoom
    {
        public int CourtActivitySlotId { get; set; }
        public string CourtRoomCd { get; set; }
        public string ActivityCd { get; set; }
        public string ActivityDisplayCd { get; set; }
        public string ActivityDsc { get; set; }
        public string RotaInitialsCd { get; set; }
        public string CourtSittingTypeCd { get; set; }
        public string AssignmentListRoomYn { get; set; }
    }
}