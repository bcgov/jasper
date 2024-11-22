using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class ActivityAppearanceResultsCollection
    {
        public ActivityAppearanceResultsCollection()
        {
            this.Items = new List<ActivityAppearanceResults>();
        }
        public List<ActivityAppearanceResults> Items { get; set; }
        public bool IsCourtListFiltered { get; set; }
        public bool isStat { get; set; }
    }

    public class ActivityAppearanceResults
    {
        public string DateStr { get; set; }
        public int? LocationId { get; set; }
        public string LocationNm { get; set; }
        public string ActivityCd { get; set; }
        public string ActivityDsc { get; set; }
        public string ActivityClassCd { get; set; }
        public string ActivityClassDsc { get; set; }
        public string[] CourtRooms { get; set; }
        public double CapacityTargetNumerator { get; set; }
        public double? CapacityTargetDenominator { get; set; }
        public int CasesTarget { get; set; }
        public double TotalHours { get; set; }
        public string CapacityConstraintCd { get; set; }
        public string CapacityConstraintDsc { get; set; }
        public List<ActivityAppearanceDetail> Appearances { get; set; }
        public List<CourtActivityDetail> CourtActivityDetails { get; set; }
        public List<CourtRoomDetail> CourtRoomDetails { get; set; }
    }

    public class CourtRoomDetail
    {
        public string CourtRoomCd { get; set; }
        public string AssignmentListRoomYn { get; set; }
        public int CasesTarget { get; set; }
        public double TotalHours { get; set; }

        public string isAM { get; set; }

        public string isPM { get; set; }
        public List<AdjudicatorDetail> adjudicatorDetails { get; set; }
    }

    public class AdjudicatorDetail
    {
        public int adjudicatorId { get; set; }
        public string adjudicatorNm { get; set; }
        public string adjudicatorInitials { get; set; }
        public string amPm { get; set; }
    }


    public class CourtActivityDetail
    {
        public int? CourtActivityId { get; set; }
        public string NoAdditionsYn { get; set; }
        public string NoAdditionsCommentTxt { get; set; }
        public string CourtSittingCd { get; set; }
    }



    public class ActivityAppearanceDetail
    {
        public int? AslSortOrder { get; set; }
        public string CourtDivisionCd { get; set; }
        public string AppearanceDt { get; set; }
        public string AppearanceTm { get; set; }
        public string CourtRoomCd { get; set; }
        public string CourtFileNumber { get; set; }
        public int? PcssAppearanceId { get; set; }

        public bool IsComplete { get; set; }

        //For the file instead of the overall type for the court activity
        //Only different from the top level if mixed
        public string ActivityClassCd { get; set; }
        public string ActivityClassDsc { get; set; }

        public string AppearanceReasonCd { get; set; }
        public string AppearanceReasonDsc { get; set; }
        public AppearanceMethod AppearanceMethod { get; set; }
        public EquipmentSearchResults EquipmentBooking { get; set; }

        public string ScheduleNoteTxt { get; set; }

        public string EstimatedTimeHour { get; set; }
        public string EstimatedTimeMin { get; set; }
        public string EstimatedTimeString { get; set; }

        public string JustinNo { get; set; }
        public string PhysicalFileId { get; set; }
        public string CourtlistRefNumber { get; set; }

        public string StyleOfCause { get; set; }
        public string AdjudicatorInitials { get; set; }
        public string AdjudicatorNm { get; set; }
        public string CaseAgeDays { get; set; }
        public string VideoYn { get; set; }
        public string AccusedNm { get; set; }
        public string AccusedCounselNm { get; set; }
        public string AppearanceId { get; set; }
        public string ProfPartId { get; set; }
        public string ProfSeqNo { get; set; }

        //Markers
        public string InCustodyYn { get; set; }
        public string DetainedYn { get; set; }
        public string ContinuationYn { get; set; }
        public string CondSentenceOrderYn { get; set; }
        public string LackCourtTimeYn { get; set; }
        public string OtherFactorsYn { get; set; }
        public string OtherFactorsComment { get; set; }
        public string CfcsaYn { get; set; }
        public string SoftYn { get; set; }

        public string ScheduledOnDt { get; set; }

        public string ScheduledByInitials { get; set; }

        public string ScheduledByName { get; set; }

        public string ActivityCd { get; set; }
        public string ActivityDsc { get; set; }

        public int? CourtActivityId { get; set; }

        public int? CourtActivitySlotId { get; set; }

        public string RemoteVideoYn { get; set; }

        public string AppearanceStatusCd { get; set; }

        public string AppearanceStatusDsc { get; set; }

        public int? TotalAppearances { get; set; }
        public int? AppearanceNumber { get; set; }

        public string TrialTrackerCd { get; set; }
        public string TrialTrackerDsc { get; set; }
        public string TrialTrackerTrialResultTxt { get; set; }
        public string TrialTrackerOtherTxt { get; set; }

        public string AslParentTrialTrackerCd { get; set; }
        public string AslParentTrialTrackerDsc { get; set; }

        public string AssignmentListRoomYn { get; set; }

        public AslChildAppearance AslChildAppearance { get; set; }
        public List<Charge> Charges { get; set; }
        public List<Crown> Crown { get; set; }

        public List<PcssCounsel> Counsel { get; set; }

        public JustinCounsel JustinCounsel { get; set; }

        public int? HomeLocationId { get; set; }
        public string HomeLocationNm { get; set; }
        public int? RemoteLocationId { get; set; }
        public string RemoteLocationNm { get; set; }
        public dynamic CeisCounsel { get; set; }

        public string JustinApprId { get; set; }

        public string CeisAppearanceId { get; set; }

        public List<JcmComment> JcmComments { get; set; }
        public List<AppearanceAdjudicatorRestriction> AppearanceAdjudicatorRestriction { get; set; }

        public string StoodDownJCMYn { get; set; }
        public string CourtClassCd { get; set; }
        public string AppearanceSequenceNumber { get; set; }

        public string AslCourtFileNumber
        {
            get
            {
                return string.IsNullOrEmpty(this.CourtClassCd) ? this.CourtFileNumber : string.Format("{0}-{1}", this.CourtClassCd, this.CourtFileNumber);
            }
        }

        public string SelfRepresentedYn { get; set; }
        public string OtherRepresentedYn { get; set; }
        public PcssCounsel LinkedCounsel { get; set; }
        public List<Adjudicator> AslFeederAdjudicators { get; set; }
    }

    public class AslChildAppearance
    {
        public string AdjudicatorInitials { get; set; }
        public string AdjudicatorNm { get; set; }
        public string CourtRoomCd { get; set; }
        public string AppearanceDt { get; set; }
        public string AppearanceTm { get; set; }
    }

    public class UpdateAslSortOrderRequest
    {
        public List<AslSortOrderAppearance> Appearances { get; set; }
    }

    public class AslSortOrderAppearance
    {
        public int? AslSortOrder { get; set; }
        public int? PcssAppearanceId { get; set; }
    }
}