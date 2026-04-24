namespace Scv.Models.Calendar;

public class CourtCalendarActivitiesSchedule
{
    public IEnumerable<CourtCalendarDay> Days { get; set; } = [];
    public IEnumerable<Activity> Activities { get; set; } = [];
}
