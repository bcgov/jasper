using System.Collections.Generic;

namespace Scv.Api.Models.Calendar;

public class CalendarDay
{
    public string Date { get; set; }
    public bool IsWeekend { get; set; }
    public bool ShowCourtList { get; set; }
    public List<CalendarDayActivity> Activities { get; set; }
}
