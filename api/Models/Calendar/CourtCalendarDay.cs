using System.Collections.Generic;

namespace Scv.Api.Models.Calendar;

public class CourtCalendarDay
{
    public string Date { get; set; }
    public bool IsWeekend { get; set; }
    public List<CourtCalendarLocation> Locations { get; set; } = [];
}
