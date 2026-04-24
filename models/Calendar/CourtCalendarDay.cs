using System.Globalization;

namespace Scv.Models.Calendar;

public class CourtCalendarDay
{
    public const string DASHBOARD_DATE_FORMAT = "dd-MMM-yyyy";
    public string Date { get; set; }
    public bool IsWeekend => DateTime.TryParseExact(
        Date, DASHBOARD_DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
        && (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday);
    public List<CourtCalendarLocation> Locations { get; set; } = [];
}
