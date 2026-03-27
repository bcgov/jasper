using System.Collections.Generic;

namespace Scv.Api.Models.Calendar;

public class CourtCalendarActivitiesSchedule
{
    public List<CourtCalendarDay> Days { get; set; } = [];
    public List<Activity> Activities { get; set; } = [];
}
