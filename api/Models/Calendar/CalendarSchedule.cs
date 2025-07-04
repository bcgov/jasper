using System.Collections.Generic;
using Scv.Api.Models.Lookup;

namespace Scv.Api.Models.Calendar
{
    public class CalendarSchedule
    {
        public CalendarDay Today { get; set; }
        public List<CalendarDay> Days { get; set; } = [];
        public List<FilterCode> Activities { get; set; } = [];
        public List<FilterCode> Presiders { get; set; } = [];
    }
}