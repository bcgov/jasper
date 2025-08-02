using System.Collections.Generic;

namespace Scv.Api.Models.Calendar;

public class CourtCalendarSchedule : CalendarSchedule
{
    public List<Activity> Activities { get; set; } = [];
    public List<Presider> Presiders { get; set; } = [];
}
