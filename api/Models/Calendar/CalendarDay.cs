using System;
using System.Collections.Generic;
using PCSSCommon.Models;

namespace Scv.Api.Models.Calendar;

public class CalendarDay : JudicialCalendarDay
{
    public string RotaInitials { get; set; }
    public DateTime Start
    {
        get
        {
            return DateTime.ParseExact(base.Date, "dd-MMM-yyyy", null).AddHours(8);
        }
    }

    public bool showAM
    {
        get
        {
            return showPM;
        }
    }
    public bool showPM
    {
        get
        {
            if (showPMLocation || (this.Assignment?.ActivityAm?.CourtRoomCode != this.Assignment?.ActivityPm?.CourtRoomCode)
                || (this.Assignment?.ActivityAm?.ActivityDescription != this.Assignment?.ActivityPm?.ActivityDescription))
                return true;
            else
                return false;
        }
    }
    public bool showPMLocation
    {
        get
        {
            if (this.Assignment?.ActivityPm != null && this.Assignment?.ActivityAm?.LocationName != this.Assignment?.ActivityPm?.LocationName)
                return true;
            else
                return false;
        }
    }
}

/// <summary>
/// Will be renamed to CalendarDay once the Calendar has been refactored
/// </summary>
public class CalendarDayV2
{
    public string Date { get; set; }
    public List<CalendarDayActivity> Activities { get; set; }
}
