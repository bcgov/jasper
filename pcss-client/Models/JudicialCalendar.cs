//----------------------
// This is manually imported from PCSS codebase because the import files used in NSwag did not include the classes found here.
//----------------------

using System.Globalization;
using PCSSCommon.Common;

namespace PCSSCommon.Models;

#pragma warning disable 8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable 8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class ReadJudicialCalendarsResponse
{
    public List<JudicialCalendar> Calendars { get; set; }
}

public class JudicialCalendar
{
    public int Id { get; set; }
    public string RotaInitials { get; set; }
    public double ParticipantId { get; set; }

    public int HomeLocationId { get; set; }
    public string HomeLocationName { get; set; }

    public string RegionCode { get; set; }
    public int? WorkAreaSequenceNo { get; set; }

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
    public bool HasAdjudicatorIssues { get { return Restrictions.Exists(x => x.HasIssue.GetValueOrDefault()); } }

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

#pragma warning restore 8600
#pragma warning restore 8618