using Newtonsoft.Json;
using PCSS.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST.JudicialCalendar
{
    public class JudicialCalendar
    {
        public int Id { get; set; }
        public string RotaInitials { get; set; }
        public double ParticipantId { get; set; }

        public int HomeLocationId { get; set; }
        public string HomeLocationName { get; set; }

        public string RegionCode { get; set; }
        public int? WorkAreaSequenceNo { get; set; }

        public string LastName { get; set; }
        public string Name { get; set; }
        public string PositionTypeCode { get; set; }
        public string PositionTypeDescription { get; set; }
        public string PositionCode { get; set; }
        public string PositionDescription { get; set; }
        public string PositionStatusCode { get; set; }
        public string PositionStatusDescription { get; set; }
        public bool IsPresider { get; set; }
        public bool IsJudge { get; set; }
        public bool IsAdmin { get; set; }

        public JudicialCalendar()
        {
            this.Days = new List<JudicialCalendarDay>();
        }

        public List<JudicialCalendarDay> Days { get; set; }
        public void AddDay(JudicialCalendarDay day)
        {
            day.JudgeId = this.Id;
            this.Days.Add(day);
        }
        public JudicialCalendarDay GetDay(DateTime date)
        {
            JudicialCalendarDay item = this.Days.Find(d => d.Date.Equals(date.ToString(Constants.DATE_FORMAT), StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = new JudicialCalendarDay(this.Id, date);
                AddDay(item);
            }
            return item;
        }

        public JudicialCalendarDay GetDay(String dateStr)
        {
            JudicialCalendarDay item = this.Days.Find(d => d.Date.Equals(dateStr, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = new JudicialCalendarDay(this.Id, DateTime.ParseExact(dateStr, Constants.DATE_FORMAT, CultureInfo.CurrentCulture));
                AddDay(item);
            }
            return item;
        }
    }

    public class JudicialCalendarDay
    {
        private JudicialCalendarAssignment _assignment;
        private List<AdjudicatorRestriction> _restrictions = new List<AdjudicatorRestriction>();
        public int JudgeId { get; set; }
        public string Date { get; set; }

        // these things can change from day to day....
        public string Name { get; set; }
        public string LastName { get; set; }
        public string PositionTypeCode { get; set; }
        public string PositionTypeDescription { get; set; }
        public string PositionCode { get; set; }
        public string PositionDescription { get; set; }
        public string PositionStatusCode { get; set; }
        public string PositionStatusDescription { get; set; }
        public bool IsPresider { get; set; }
        public bool IsJudge { get; set; }
        public bool IsAdmin { get; set; }

        public List<AdjudicatorRestriction> Restrictions { get { return _restrictions; } }
        public bool HasRestrictions { get { return Restrictions.Count > 0; } }
        public bool HasAdjudicatorIssues { get { return Restrictions.Exists(x => x.HasIssue); } }

        public List<HaveJudgeResponse> HaveJudgeDetails { get; set; }

        public JudicialCalendarDay() { }

        public JudicialCalendarDay(int judgeId, DateTime date)
        {
            this.JudgeId = judgeId;
            this.Date = date.ToString(Constants.DATE_FORMAT);
        }

        public JudicialCalendarAssignment Assignment
        {
            get { return _assignment; }
            set
            {
                value.JudgeId = this.JudgeId;
                value.Date = this.Date;
                this._assignment = value;
            }
        }

        public DateTime GetDate()
        {
            return DateTime.ParseExact(Date, Constants.DATE_FORMAT, CultureInfo.CurrentCulture);
        }

    }

    public class JudicialCalendarAssignment
    {
        public int? Id { get; set; }
        public int? TentativeScheduleId { get; set; }
        public string TentativeScheduleName { get; set; }

        public int JudgeId { get; set; }

        public int? LocationId { get; set; }
        public string LocationName { get; set; }

        public string Date { get; set; }

        public string ActivityCode { get; set; }
        public string ActivityDisplayCode { get; set; }
        public string ActivityDescription { get; set; }
        public bool IsCommentRequired { get; set; }

        public string ActivityClassCode { get; set; }
        public string ActivityClassDescription { get; set; }

        public string Comments { get; set; }
        public bool IsVideo { get; set; }
        public int? FromLocationId { get; set; }
        public string FromLocationName { get; set; }
        public bool IsExtraSeniorDay { get; set; }

        public bool Force { get; set; } // force this assignment regardless of GNSD or Weekend.
        public bool IgnoreWeekendUpdate { get; set; } // don't validate GSND or weekend, as we won't save them...

        public JudicialCalendarActivity ActivityAm { get; set; }
        public JudicialCalendarActivity ActivityPm { get; set; }

        [JsonProperty(PropertyName = "IsJj")]
        public bool IsJJ { get; set; }
        [JsonProperty(PropertyName = "IsPcj")]
        public bool IsPCJ { get; set; }
        [JsonProperty(PropertyName = "IsJp")]
        public bool IsJP { get; set; }
        [JsonProperty(PropertyName = "IsOther")]
        public bool IsOther { get { return !(IsJJ || IsPCJ || IsJP || IsIAR); } }
        [JsonProperty(PropertyName = "IsIar")]
        public bool IsIAR { get { return !(IsJJ || IsPCJ || IsJP) && "IA" == this.ActivityCode; } }
        [JsonIgnore]
        public bool IsPublished { get; set; }

        // only used on save...
        public bool RemoveFromActivityAm { get; set; }
        public bool RemoveFromActivityPm { get; set; }
        public string UpdateDate { get; set; }

        public string UpdateTime { get; set; }
        public string UpdJcmInitials { get; set; }
    }
    public class JudicialCalendarActivity
    {
        public int CourtActivityId { get; set; }
        public string ActivityCode { get; set; }
        public string ActivityDescription { get; set; }
        public string ActivityDisplayCode { get; set; }
        public string ActivityClassCode { get; set; }
        public string ActivityClassDescription { get; set; }
        public string JudiciaryTypeCode { get; set; }

        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public int? FromLocationId { get; set; }
        public string FromLocationName { get; set; }
        public string CourtRoomCode { get; set; }
        public string CourtSittingCode { get; set; }

        public bool IsVideo { get { return this.FromLocationId != null && this.FromLocationId > 0; } }
        public bool IsJJ { get { return "JJ" == this.JudiciaryTypeCode; } }
        public bool IsPCJ { get { return "PCJ" == this.JudiciaryTypeCode; } }
        public bool IsJP { get { return "JP" == this.JudiciaryTypeCode; } }
        public bool IsOther { get { return !(IsJJ || IsPCJ || IsJP || IsIAR); } }
        public bool IsIAR { get { return !(IsJJ || IsPCJ || IsJP) && "IA" == this.ActivityCode; } }
        public bool IsWithinLookaheadWindow { get; set; }

    }

    public class Case
    {
        //Both
        public string FileNumberTxt { get; set; }
        public int LocationId { get; set; }
        public string LocationNm { get; set; }

        public string NextApprDt { get; set; }
        public string CourtDivisionCd { get; set; }
        public List<Participant> Participants { get; set; }

        //Criminal Only
        public int? JustinNo { get; set; }
        public double? ProfPartId { get; set; }
        public double? ProfSeqNo { get; set; }

        //Civil Only
        public double? PhysicalFileId { get; set; }
        public double? CivilDocumentId { get; set; }
    }

    public class Participant
    {
        public string FullName { get; set; }
        public List<Charge> Charges { get; set; }
    }

    public class HaveJudgeResponse
    {
 
        public int? HaveJudgeId { get; set; }
        public int? JudiciaryPersonId { get; set; }
        public int? JudicialScheduleId { get; set; }
        public string RotaInitialsCd { get; set; }
        public string FullNm { get; set; }
        public int? LocationId { get; set; }
        public string LocationNm { get; set; }
        public DateTime CalendarDt { get; set; }
        public string CourtSittingCd { get; set; }
        public virtual string CourtSittingDsc { get; set; }
        public string HaveJudgeTypeCd { get; set; }
        public virtual string HaveJudgeTypeDsc { get; set; }
    }
}
