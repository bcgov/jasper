using System.Collections.Generic;

namespace Scv.Api.Models.Calendar;

public enum Period
{
    AM,
    PM
}

public class CalendarDayActivity
{
    public int? LocationId { get; set; }
    public string LocationName { get; set; }
    public string LocationShortName { get; set; }
    public string ActivityCode { get; set; }
    public string ActivityDisplayCode { get; set; }
    public string ActivityDescription { get; set; }
    public string ActivityClassCode { get; set; }
    public string ActivityClassDescription { get; set; }
    public bool IsRemote { get; set; }
    public bool ShowCourtList { get; set; }
    public string RoomCode { get; set; }
    public Period? Period { get; set; }
    public int FilesCount { get; set; }
    public int ContinuationsCount { get; set; }
    public List<AdjudicatorRestriction> Restrictions { get; set; } = [];
}
