using Scv.Api.Models.Lookup;
using System.Collections.Generic;

namespace Scv.Api.Models.Calendar
{
    /// <summary>
    /// Signle class for Calendar and 2 left panels for filtering
    /// </summary>
    public class CalendarSchedule
    {
                public List<CalendarDay> Schedule { get; set; }
                public List<FilterCode> Activities { get; set; }
                public List<FilterCode> Presiders { get; set; }
    }
}