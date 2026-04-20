using System;
using System.Collections.Generic;
using System.Globalization;
using Scv.Api.Services;

namespace Scv.Api.Models.Calendar;

public class CourtCalendarDay
{
    public string Date { get; set; }
    public bool IsWeekend => DateTime.TryParseExact(
        Date, DashboardService.DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
        && (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday);
    public List<CourtCalendarLocation> Locations { get; set; } = [];
}
