using System.Collections.Generic;

namespace Scv.Api.Models.Calendar;

public class CourtCalendarSchedule
{
    public List<CalendarDay> Days { get; set; } = [];
    public List<Activity> Activities { get; set; } = [];
    public List<Presider> Presiders { get; set; } = [];
}
