using System.Collections.Generic;

namespace Scv.Api.Models.Calendar;

public class CalendarDay
{
    public string Date { get; set; }
    public bool IsWeekend { get; set; }
    public bool ShowCourtList { get; set; }
    public IEnumerable<CalendarDayActivity> Activities { get; set; }
}
