using PCSS.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{

    public class RequiredPerson
    {
        public string PartId { get; set; }
        public string PersonTypeCd { get; set; }
    }

    public class ActivityType
    {
        public string ActivityCd { get; set; }
        public string ActivityClassCd { get; set; }
    }

    public class FindBestDateParameters
    {
        public FindBestDateParameters()
        {
            RequiredPersonnel = new List<RequiredPerson>();
        }

        public int LocationId { get; set; }

        public double? AdjPartId { get; set; }

        public double? EstimatedQty { get; set; }
        public string EstimatedUnitCd { get; set; }

        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int? JustinNo { get; set; }
        public double? PhysicalFileId { get; set; }
        public List<RequiredPerson> RequiredPersonnel { get; set; }
        public List<int> CounselIds { get; set; }

        public List<string> ActivityCds { get; set; }

        public string BestDateYn { get; set; }
        public string IncludeAppearancesYn { get; set; }



    }

    public class FindBestDateResult
    {
        public FindBestDateResult()
        {
            PersonnelAvailability = new List<PersonnelAvailability>();
            OfferedDates = new List<OfferedDate>();
            CounselAvailability = new List<CounselAvailability>();
            Restrictions = new List<AdjudicatorRestriction>();
            Appearances = new List<BestDateAppearanceDetail>();
        }
        public DateTime Date { get; set; }
        public string DateStr { get; set; }

        public bool CapacityFlag { get; set; }
        public double? CapacityScore { get; set; }

        public bool WitnessFlag { get; set; }
        public double WitnessScore { get; set; }
        public bool JudgeFlag { get; set; }
        public bool AdditionsAllowedFlag { get; set; }
        public string ClosedComments { get; set; }
        public bool WrongDurationFlag { get; set; }
        public bool CounselAvailabilityFlag { get; set; }
        public bool CrownCounselAvailabilityFlag { get; set; }
        public bool BestDateFlag { get; set; }

        public double? TotalQuantity { get; set; }
        public double? UsedQuantity { get; set; }
        public int? NumberOfCases { get; set; }
        public double? NumberOfHours { get; set; }
        
        public List<PersonnelAvailability> PersonnelAvailability { get; set; }
        public List<ActivityClassUsage> ActivityClassUsages { get; set; }
        public string AvailabilityCd
        {
            get
            {
                return CounselAvailability.Any(x => x.Details != null && x.Details.Any()) ? Constants.AVAIL_PROV_COURT : Constants.AVAIL_UNKNOWN;
            }
        }
        public string ActivityCd { get; set; }
        public string ActivityCdDsc { get; set; }
        public string CourtRoomCd { get; set; }

        public string CapacityConstraintCd { get; set; }
        public List<OfferedDate> OfferedDates { get; set; }
        public List<CounselAvailability> CounselAvailability { get; set; }
        public JudgeAvailability JudgeAvailability { get; set; }
        public List<AdjudicatorRestriction> Restrictions { get; set; }
        public bool HasRestrictions { get { return Restrictions.Count > 0; } }
        public bool HasAdjudicatorIssues { get { return Restrictions.Exists(x => x.HasIssue); } }
        
        // task2658
        public List<BestDateAppearanceDetail> Appearances { get; set; }


        public bool CompletedPersonnelSearch { get; set; }
    }

    public class ActivityClassUsage
    {
        public string ActivityClassCd { get; set; }
        public int? NumberOfCases { get; set; }
        public double? NumberOfHours { get; set; }

        public string ActivityClassDsc { get; set; }

        public double CapacityScore { get; set; }

        public double TotalQuantity { get; set; }

        public double UsedQuantity { get; set; }
    }

    public class GetPersonnelAvailabilityParameters
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string AssignmentDate { get; set; }
        public List<RequiredPerson> RequiredPersonnel { get; set; }
        public int? LocationId { get; set; }

    }

    public class GetCounselAvailabilityParameters
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }        
        public List<int> CounselIds { get; set; }
    }

    public class GetJudgeAvailabilityParameters
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public double AdjPartId { get; set; }
        public int LocationId { get; set; }
        public string ActivityCd { get; set; }
    }

    public class Assignment
    {
        public string AssignmentTypeDsc { get; set; }
        public string CreateDt { get; set; }
        public string StartDt { get; set; }
        public string EndDt { get; set; }
        public string PoliceAgencyDsc { get; set; }        
    }

    public class Commitment
    {
        public string CommitmentTypeDsc { get; set; }

        public string ActivityTypeCd { get; set; }
        public string ActivityTypeDsc { get; set; }

        public string CreatedDt { get; set; }
        public string CourtAgencyId { get; set; }
        public int? LocationId { get; set; }
        public string LocationNm { get; set; }
        public string RegionNm { get; set; }
        public string CourtRoomCd { get; set; }
        public string CommitmentDt { get; set; }
        public string CommitmentTm { get; set; }
        public string DurationHour { get; set; }
        public string DurationMin { get; set; }
        public string CourtFileNo { get; set; }
        public string CommitmentTxt { get; set; }

        
    }

    public class PersonnelAssignmentDetail
    {
        public string PartId { get; set; }
        public string AssignmentDt { get; set; }
        public string DutyDsc { get; set; }
        public string ShiftLadderDsc { get; set; }
        public List<Assignment> Assignments { get; set; }
        public List<Commitment> Commitments { get; set; }
    }

    public class GetPersonnelAvailabilityResult
    {
        public GetPersonnelAvailabilityResult()
        {
            PersonnelDetails = new List<PersonnelAvailabilityAndAssignmentDetails>();
            WitnessScores = new List<PersonnelWitnessScores>();
        }

        public List<PersonnelAvailabilityAndAssignmentDetails> PersonnelDetails { get; set; }
        public List<PersonnelWitnessScores> WitnessScores { get; set; }
    }

    public class PersonnelWitnessScores
    {
        public DateTime Date { get; set; }
        public double WitnessScore { get; set; }
    }

    public class PersonnelAvailabilityAndAssignmentDetails
    {
        public string FullNm { get; set; }
        public string PartId { get; set; }
        public string PersonTypeCd { get; set; }
        public List<PersonnelAvailability> PersonnelAvailability { get; set; }
        public List<PersonnelAssignmentDetail> AssignmentDetails { get; set; }

        public string PinCodeTxt { get; set; }

        public string AgencyDsc { get; set; }

        public string AgencyCd { get; set; }
    }

    public class Personnel
    {
        public string LastNm { get; set; }
        public string FirstNm { get; set; }
        public string PinTxt { get; set; }
        public string PartId { get; set; }

        public string AgencyDsc { get; set; }

        public string AgencyCd { get; set; }
    }

    public class CounselAvailabilityResult
    {
        public int CounselId { get; set; }
        public int? LawSocietyId { get; set; }
        public string LastNm { get; set; }
        public string GivenNm { get; set; }
        public string PrefNm { get; set; }
        public string FullNm { get { return LastNm + ", " + GivenNm; } }
        public List<CounselAvailability> CounselAvailabilities { get; set; }       
    }

    public class CounselAvailability
    {
        public int CounselId { get; set; }
        public int? LawSocietyId { get; set; }
        public string LastNm { get; set; }
        public string GivenNm { get; set; }
        public string PrefNm { get; set; }
        public string FullNm { get { return LastNm + ", " + GivenNm; } }
        public string OrgNm { get; set; }
        public DateTime Date { get; set; }
        public string DateStr { get; set; }
        public List<CounselAvailabilityDetail> Details { get; set; }
        public string AvailabilityCd
        {
            get
            {
                return (Details != null && Details.Any()) ? Constants.AVAIL_PROV_COURT : Constants.AVAIL_UNKNOWN;
            }
        }
    }

    public class CounselAvailabilityDetail
    {
        public string AppearanceTm { get; set; }
        public int? LocationId { get; set; }
        public string LocationNm { get; set; }
        public string CourtRoomCd { get; set; }
        public double? EstimatedQty { get; set; }
        public string EstimatedUnitCd { get; set; }

        public string CourtFileNumber { get; set; }
        public int? PcssAppearanceId { get; set; }
        public string JustinNo { get; set; }
        public string PhysicalFileId { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var t = obj as CounselAvailabilityDetail;
            if (t == null) return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hash = GetType().GetHashCode();
            return hash;
        }
    }

    public class JudgeAvailabilityResult
    {
        public string FullNm { get; set; }
        public double AdjPartId { get; set; }
        public string HomeLocationSNm { get; set; }
        public List<JudgeAvailability> JudgeAvailabilities { get; set; }
    }

    public class JudgeAvailability
    {
        public JudgeAvailability()
        {
            AssignmentDetails = new List<JudgeAssignmentDetail>();
            Restrictions = new List<AdjudicatorRestriction>();
        }

        public string FullNm { get; set; }
        public double AdjPartId { get; set; }
        public string HomeLocationSNm { get; set; }
        public string AvailabilityCd { get; set; }
        public string AvailabilityDsc { get; set; }
        public DateTime Date { get; set; }
        public string DateStr { get; set; }
        public List<JudgeAssignmentDetail> AssignmentDetails { get; set; }
        public List<AdjudicatorRestriction> Restrictions { get; set; }
    }    

    public class JudgeAssignmentDetail
    {
        public int? LocationId { get; set; }
        public string LocationNm { get; set; }
        public string JudgeActivityCd { get; set; }
        public string JudgeActivityDsc { get; set; }
        public string CourtActivityCd { get; set; }
        public string CourtActivityDsc { get; set; }
        public int? CourtLocationId { get; set; }
        public string CourtLocationNm { get; set; }
        public string CourtRoomCd { get; set; }
        public string CommentTxt { get; set; }
        public string VideoYn { get; set; }
        public string CourtSittingCd { get; set; }       
    }


    public class BestDateAppearanceDetail
    {
        public BestDateAppearanceDetail()
        {
            AppearanceAdjudicatorRestriction = new List<AppearanceAdjudicatorRestriction>();
        }
        public int LocationId { get; set; }
        public string CourtRoomCd { get; set; }

        public int AppearanceId { get; set; }
        public string AppearanceDt { get; set; }
        public string AppearanceTm { get; set; }
        
        public string CourtFileNumber { get; set; }
        public string CourtDivisionCd { get; set; }
        
        public string ActivityCd { get; set; }

        public string ActivityClassCd { get; set; }

        public string AppearanceReasonCd { get; set; }

        public int TotalAppearances { get; set; }
        public int AppearanceNumber { get; set; }

        public string EstimatedTimeHour { get; set; }
        public string EstimatedTimeMin { get; set; }
        public string EstimatedTimeString { get; set; }
        public string JustinNo { get; set; }
        public string PhysicalFileId { get; set; }

        public List<AppearanceAdjudicatorRestriction> AppearanceAdjudicatorRestriction { get; set; }
    }
}
