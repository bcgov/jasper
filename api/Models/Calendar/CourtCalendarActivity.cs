
using System.Collections.Generic;

namespace Scv.Api.Models.Calendar;

public class CourtCalendarActivity
{
    public string ActivityCode { get; set; }
    public string ActivityDescription { get; set; }
    public string ActivityClassCode { get; set; }
    public string ActivityClassDescription { get; set; }

    public List<string> CourtRooms { get; set; } = [];
}
