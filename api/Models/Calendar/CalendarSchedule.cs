using System.Collections.Generic;

namespace Scv.Api.Models.Calendar
{
    public class CalendarSchedule
    {
        public CalendarDay Today { get; set; }
        public List<CalendarDay> Days { get; set; } = [];
    }
}