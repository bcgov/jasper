using Newtonsoft.Json;
using PCSS.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST.CourtCalendar
{

    public class CourtCalendarLocation
    {

        public int Id { get; set; }
        public string Name { get; set; }

        public string AgencyIdentifierCode { get; set; }

        public string RegionCode { get; set; }
        public int? WorkAreaSequenceNo { get; set; }

        public bool IsActive { get; set; }
        public bool IsGroupParent { get; set; }

        public string PublishDate { get; set; }
        public string GenerationDate { get; set; }

        public List<CourtCalendarDay> Days { get; set; }

        public CourtCalendarLocation()
        {
            this.Days = new List<CourtCalendarDay>();
        }

        public DateTime GetStartDate()
        {
            return DateTime.ParseExact(this.Days[0].Date, Constants.DATE_FORMAT, CultureInfo.CurrentCulture);
        }
        public DateTime GetEndDate()
        {
            return DateTime.ParseExact(this.Days[this.Days.Count - 1].Date, Constants.DATE_FORMAT, CultureInfo.CurrentCulture);
        }

        public int CourtRoomConflictDayCount
        {
            get
            {
                return this.Days.Where(x => x.CourtRoomConflicts.Count > 0).Count();
            }
        }

        public int ActivityImbalanceDayCount
        {
            get
            {
                return this.Days.Where(x => x.HasActivityImbalance).Count();
            }
        }

        public int JudicialImbalanceDayCount
        {
            get
            {
                return this.Days.Where(x => x.HasJudicialImbalance).Count();
            }
        }

        public int AdjudicatorRestrictionIssuesDayCount
        {
            get
            {
                return this.Days.Where(x => x.HasAdjudicatorRestrictionIssues).Count();
            }
        }

        public void AddDay(CourtCalendarDay item)
        {
            item.LocationId = this.Id;
            this.Days.Add(item);
        }

        public CourtCalendarDay GetDay(DateTime date)
        {
            CourtCalendarDay item = this.Days.Find(d => d.Date.Equals(date.ToString(Constants.DATE_FORMAT), StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = new CourtCalendarDay(this.Id, date);
                AddDay(item);
            }
            return item;
        }

        public void CalculateCourtRoomConflicts()
        {
            // flatten out days/sittings/rooms and activities...
            List<CourtCalendarConflict> items = new List<CourtCalendarConflict>();
            foreach (CourtCalendarDay day in this.Days)
            {
                foreach (CourtCalendarActivity activity in day.Activities)
                {
                    foreach (CourtCalendarSlot slot in activity.Slots.Where(x => x.IsAssignmentListRoom == false))
                    {
                        CourtCalendarConflict item = items.Find(x => (x.LocationId == this.Id) && (x.Date == day.Date) && (x.CourtSittingCode == activity.CourtSittingCode) && (x.CourtRoomCode == slot.CourtRoomCode));
                        if (item == null)
                        {
                            item = new CourtCalendarConflict()
                            {
                                LocationId = this.Id,
                                Name = this.Name,
                                Date = day.Date,
                                CourtSittingCode = activity.CourtSittingCode,
                                CourtRoomCode = slot.CourtRoomCode
                            };
                            items.Add(item);
                        }
                        if (!item.ActivityCodes.Contains(activity.ActivityCode))
                        {
                            item.ActivityCodes.Add(activity.ActivityCode);
                        }
                    }
                }
            }
            //
            // ok, now for each day update set the conflicts and mark the location flag (if any conflicts)...
            //
            foreach (CourtCalendarDay day in this.Days)
            {
                day.CourtRoomConflicts = items.Where(x => (x.ActivityCodes.Count > 1) && (x.Date == day.Date)).ToList();
            }
        }

   
    }

    public class CourtCalendarDay
    {
        private CourtCalendarActivityImbalance _activityImbalance;
        private CourtCalendarJudicialImbalance _judicialImbalance;
        private List<CourtCalendarActivity> _activities = new List<CourtCalendarActivity>();
        private List<CourtCalendarConflict> _conflicts = new List<CourtCalendarConflict>();

        public int LocationId { get; set; }
        public string Date { get; set; }
        public List<DayLocationNote> DayLocationNotes { get; set; }

        public int PcjRequired { get; set; }
        public int PcjMinimum { get; set; }
        public int PcjMaximum { get; set; }

        public CourtCalendarDay() {
            this._activityImbalance = new CourtCalendarActivityImbalance();
            this._judicialImbalance = new CourtCalendarJudicialImbalance();
            TrialTrackingMissingCount = 0;
        }

        public CourtCalendarDay(int locationId, DateTime date)
        {
            this.LocationId = locationId;
            this.Date = date.ToString(Constants.DATE_FORMAT);
            this._activityImbalance = new CourtCalendarActivityImbalance() { LocationId = this.LocationId, Date = this.Date };
            this._judicialImbalance = new CourtCalendarJudicialImbalance() { LocationId = this.LocationId, Date = this.Date };
            TrialTrackingMissingCount = 0;
        }

        public DateTime GetDate()
        {
            return DateTime.ParseExact(Date, Constants.DATE_FORMAT, CultureInfo.CurrentCulture);
        }

        public List<CourtCalendarActivity> Activities { get { return _activities; } }

        public List<CourtCalendarConflict> CourtRoomConflicts { get { return _conflicts; } set { _conflicts = value; } }
        public CourtCalendarActivityImbalance ActivityImbalance
        {
            get { return (_activityImbalance.IsBalanced) ? null : _activityImbalance; }
            set { if (value != null) this._activityImbalance = value; }
        }
        public CourtCalendarJudicialImbalance JudicialImbalance
        {
            get { return (_judicialImbalance.IsBalanced) ? null : _judicialImbalance; }
            set { if (value != null) this._judicialImbalance = value; }
        }
        public bool HasActivityImbalance { get { return !_activityImbalance.IsBalanced; } }
        public bool HasJudicialImbalance { get { return !_judicialImbalance.IsBalanced; } }
        public bool IsReconciliationRequired { get; set; }
        public bool HasAdjudicatorRestrictionIssues { get { return _activities.Exists(x => x.HasAdjudicatorIssues); } }

        public void AddActivity(CourtCalendarActivity item)
        {
            item.Date = this.Date;
            item.LocationId = this.LocationId;
            _activities.Add(item);
        }

        public CourtCalendarActivity GetActivity(string activityCode, string courtSittingCode)
        {
            CourtCalendarActivity item = _activities.Find(a => a.ActivityCode.Equals(activityCode, StringComparison.OrdinalIgnoreCase) && a.CourtSittingCode.Equals(courtSittingCode, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = new CourtCalendarActivity() { ActivityCode = activityCode, CourtSittingCode = courtSittingCode };
                AddActivity(item);
            }
            return item;
        }


        public int PcjActivitiesCountAm { get; set; }

        public int PcjActivitiesCountPm { get; set; }

        public int PcjSittingCount { get; set; }

        public int TrialTrackingMissingCount { get; set; }
    }

    public class CourtCalendarActivity
    {
        private List<CourtCalendarSlot> _slots = new List<CourtCalendarSlot>();
        private List<CourtCalendarCapacity> _capacitySettings = new List<CourtCalendarCapacity>();
        private List<AdjudicatorRestriction> _restrictions = new List<AdjudicatorRestriction>();
        public int? Id { get; set; }
        public int LocationId { get; set; }
        public string Date { get; set; }

        public string ActivityCode { get; set; }
        public string ActivityDescription { get; set; }

        public string ActivityClassCode { get; set; }
        public string ActivityClassDescription { get; set; }

        public string CourtSittingCode { get; set; }
        public string CapacityConstraintCode { get; set; }

        public double? PCJRequiredQuantity { get; set; }
        public string JudiciaryTypeCode { get; set; }

        [JsonProperty(PropertyName = "IsJj")]
        public bool IsJJ { get { return "JJ" == this.JudiciaryTypeCode; } }
        [JsonProperty(PropertyName = "IsPcj")]
        public bool IsPCJ { get { return "PCJ" == this.JudiciaryTypeCode; } }
        [JsonProperty(PropertyName = "IsJp")]
        public bool IsJP { get { return "JP" == this.JudiciaryTypeCode; } }
        [JsonProperty(PropertyName = "IsOther")]
        public bool IsOther { get { return !(IsJJ || IsPCJ || IsJP || IsIAR); } }
        [JsonProperty(PropertyName = "IsIar")]
        public bool IsIAR { get { return !(IsJJ || IsPCJ || IsJP) && "IA" == this.ActivityCode; } }
        public bool IsHearingStartSameTime { get; set; }
        public bool IsPreCourtActivity { get; set; }

        public bool IsStartLessThanDay { get; set; }
        public bool IsStartSingleDay { get; set; }
        public bool IsStartMultiDayLong { get; set; }
        public bool IsStartMultiDayShort { get; set; }

        public bool IsClosedForBooking { get; set; }
        public string ClosedComments { get; set; }

        public double? CurrentCapacityPercentage { get; set; }
        public double? CurrentCapacity { get; set; }
        public double? TotalCapacity { get; set; }
        public int? NumberOfCases { get; set; }
        public double? NumberOfHours { get; set; }
        public List<ActivityClassUsage> ActivityClassUsages { get; set; }

        public List<AdjudicatorRestriction> Restrictions { get{ return _restrictions; } }
        public bool HasRestrictions { get { return Restrictions.Count > 0; } }
        public bool HasAdjudicatorIssues { get { return Restrictions.Exists(x => x.HasIssue); } }

        public List<NeedJudgeResponse> NeedJudgeDetails { get; set; }

        public CourtCalendarActivity() { this.NeedJudgeDetails = new List<NeedJudgeResponse>(); }
        public CourtCalendarActivity(int locationId, DateTime date, string activityCode, string activityDesc, string activityClassCode, string activityClassDesc)
        {
            this.LocationId = locationId;
            this.Date = date.ToString(Constants.DATE_FORMAT);
            this.ActivityCode = activityCode;
            this.ActivityDescription = activityDesc;
            this.ActivityClassCode = activityClassCode;
            this.ActivityClassDescription = activityClassDesc;
            this.NeedJudgeDetails = new List<NeedJudgeResponse>();
        }

        public List<CourtCalendarSlot> Slots { get { return _slots; } }

        public void AddSlot(CourtCalendarSlot item)
        {
            item.CourtCalendarActivityId = this.Id;
            item.LocationId = this.LocationId;
            // only add a slot once...
            if (_slots.Find(x => x.CourtRoomCode == item.CourtRoomCode && x.IsAssignmentListRoom == item.IsAssignmentListRoom && x.StartTime == item.StartTime) == null)
            {
                _slots.Add(item);
            }
        }

        public void RemoveSlot(CourtCalendarSlot item)
        {
            _slots.Remove(item);

        }

        public List<CourtCalendarCapacity> CapacitySettings { get { return _capacitySettings; } }

        public void AddCapacity(CourtCalendarCapacity item)
        {
            item.CourtCalendarActivityId = this.Id;
            _capacitySettings.Add(item);
        }

        public void RemoveCapacity(CourtCalendarCapacity item)
        {
            _capacitySettings.Remove(item);
        }


    }

    public class CourtCalendarCapacity
    {
        public int? Id { get; set; }
        public int? CourtCalendarActivityId { get; set; }

        public string ActivityClassCode { get; set; }
        public double? Quantity { get; set; }
    }

    public class CourtCalendarSlot
    {
        public int? Id { get; set; }
        public int? CourtCalendarActivityId { get; set; }

        public int? LocationId { get; set; }
        public string CourtRoomCode { get; set; }
        public bool IsAssignmentListRoom { get; set; }
        public string StartTime { get; set; }
        public int? JudicialScheduleId { get; set; }
    }

    public class CourtCalendarConflict
    {
        public int LocationId { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }
        public string CourtRoomCode { get; set; }
        public string CourtSittingCode { get; set; }
        public List<string> ActivityCodes { get; set; }

        public CourtCalendarConflict() { this.ActivityCodes = new List<string>(); }
    }

    public class CourtCalendarActivityImbalance
    {
        public int LocationId { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }

        public int ActivityCount { get; set; }
        public List<string> PcjActivityCodes { get; set; }

        public int PcjScheduled { get; set; }

        public int PcjMinimum { get; set; }
        public int PcjMaximum { get; set; }

        public bool IsAboveRange { get { return this.ActivityCount > this.PcjMaximum; } }
        public bool IsBelowRange { get { return this.ActivityCount < this.PcjMinimum; } }
        public bool IsBalanced { get { return !IsAboveRange && !IsBelowRange;} }


        public CourtCalendarActivityImbalance() { this.PcjActivityCodes = new List<string>(); }
    }

    public class CourtCalendarJudicialImbalance
    {
        public int LocationId { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }

        public int ActivityCount { get; set; }
        public List<string> SittingActivityCodes { get; set; }

        public int PcjSitting { get; set; }

        public int PcjMinimum { get; set; }
        public int PcjMaximum { get; set; }

        public bool IsAboveRange { get { return this.PcjSitting > this.PcjMaximum; } }
        public bool IsBelowRange { get { return this.PcjSitting < this.PcjMinimum; } }
        public bool IsBalanced { get { return !IsAboveRange && !IsBelowRange;} }

        public CourtCalendarJudicialImbalance() { this.SittingActivityCodes = new List<string>(); }
    }

    public class PresiderQuantityRange
    {
        public int Quantity { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class DayLocationNote
    {
        public int? DayLocationNoteId { get; set; }
        public string NoteDt { get; set; }
        public int LocationId { get; set; }
        public string NoteTxt { get; set; }
        public string EntDtm { get; set; }
        public string EntUserName { get; set; }
        public string UpdDtm { get; set; }
        public string UpdUserName { get; set; }
        public string UpdName { get; set; }
    }

    public class Utils
    {

        public static int DayOfCourtCalendarWeek(DateTime dt)
        {
            // our Court Calendar starts with Monday as the First day of the week...
            switch (dt.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    return 1;
                case DayOfWeek.Tuesday:
                    return 2;
                case DayOfWeek.Wednesday:
                    return 3;
                case DayOfWeek.Thursday:
                    return 4;
                case DayOfWeek.Friday:
                    return 5;
                case DayOfWeek.Saturday:
                    return 6;
                default:
                    // must be sunday
                    return 7;
            }
        }


    }



    public class NeedJudgeResponse
    {
   
        public int? NeedJudgeId { get; set; }
        public int? LocationId { get; set; }
        public int? CourtActivityId { get; set; }
        public string ActivityCd { get; set; }
        public string ActivityDsc { get; set; }
        public string LocationNm { get; set; }
        public DateTime CalendarDt { get; set; }
        public string CourtSittingCd { get; set; }
        public virtual string CourtSittingDsc { get; set; }
        public string CourtRoomCd { get; set; }
        public string NeedJudgeTypeCd { get; set; }
        public virtual string NeedJudgeTypeDsc { get; set; }
    }
}