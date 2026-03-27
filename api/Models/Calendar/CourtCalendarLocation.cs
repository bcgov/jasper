using System.Collections.Generic;

namespace Scv.Api.Models.Calendar;

public class CourtCalendarLocation
{
    public string LocationId { get; set; }
    public string LocationShortName { get; set; }

    public List<CourtCalendarActivity> Activities { get; set; } = [];
}
