using Scv.Api.Models.Lookup;
using System.Collections.Generic;

namespace Scv.Api.Models.Calendar
{
    public class CalendarSchedule
    {
                public List<CalendarDay> Schedule { get; set; }
                public List<FilterCode> Activities { get; set; }
                public List<FilterCode> Presiders { get; set; }
    }
}