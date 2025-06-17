using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using PCSSCommon.Clients.JudicialCalendarServices;
using PCSSCommon.Clients.SearchDateServices;
using PCSSCommon.Models;
using Scv.Api.Infrastructure;
using Scv.Api.Models.Calendar;

namespace Scv.Api.Services;

public interface IDashboardService
{
    Task<OperationResult<CalendarSchedule>> GetMyScheduleAsync(int judgeId, string currentDate, string startDate, string endDate);
}

public class DashboardService(
    IAppCache cache,
    JudicialCalendarServicesClient calendarClient,
    SearchDateClient searchDateClient) : ServiceBase(cache), IDashboardService
{
    public const string DATE_FORMAT = "dd-MMM-yyyy";

    private readonly JudicialCalendarServicesClient _calendarClient = calendarClient;
    private readonly SearchDateClient _searchDateClient = searchDateClient;

    public override string CacheName => nameof(DashboardService);

    public async Task<OperationResult<CalendarSchedule>> GetMyScheduleAsync(int judgeId, string currentDate, string startDate, string endDate)
    {
        // Validate dates
        var isValidCurrentDate = DateTime.TryParseExact(currentDate, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var validCurrentDate);
        var isValidStartDate = DateTime.TryParseExact(startDate, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var validStartDate);
        var isValidEndDate = DateTime.TryParseExact(endDate, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var validEndDate);

        if (!isValidCurrentDate || !isValidStartDate || !isValidEndDate)
        {
            return OperationResult<CalendarSchedule>.Failure("currentDate, startDate and/or endDate is invalid.");
        }

        var formattedCurrentDate = validCurrentDate.ToString(DATE_FORMAT);
        var formattedStartDate = validStartDate.ToString(DATE_FORMAT);
        var formattedEndDate = validEndDate.ToString(DATE_FORMAT);

        async Task<JudicialCalendar> MySchedule() => await _calendarClient.ReadCalendarV2Async(judgeId, formattedStartDate, formattedEndDate);

        var myScheduleTask = this.GetDataFromCache($"{this.CacheName}-{judgeId}-{formattedStartDate}-{formattedEndDate}", MySchedule);

        var mySchedule = await myScheduleTask;

        var days = GetDays(mySchedule);

        return OperationResult<CalendarSchedule>.Success(new CalendarSchedule
        {
            Days = days,
            Today = await GetTodaysSchedule(judgeId, formattedCurrentDate, days)
        });
    }

    private async Task<CalendarDayV2> GetTodaysSchedule(int judgeId, string currentDate, List<CalendarDayV2> days)
    {
        var today = days.SingleOrDefault(d => d.Date == currentDate);
        if (today == null)
        {
            return null;
        }

        foreach (var activity in today.Activities)
        {
            // Query Court List for each activity to get the scheduled files count.
            var courtList = await _searchDateClient.GetCourtListAppearancesAsync(
                activity.LocationId.GetValueOrDefault(),
                currentDate,
                judgeId,
                activity.RoomCode,
                null);

            // Get the File count of the current Activity, Room and Judge
            var result = courtList.Items
                .FirstOrDefault(cl => cl.ActivityCd == activity.ActivityCode
                    && cl.CourtRoomDetails
                        .Any(crd => crd.CourtRoomCd == activity.RoomCode
                            && crd.AdjudicatorDetails.Any(ad => ad.AdjudicatorId == judgeId)));
            if (result != null)
            {
                activity.FilesCount = result.CasesTarget.GetValueOrDefault();
                activity.ContinuationsCount = result.Appearances.Count(a => a.ContinuationYn == "Y");
            }
        }

        return today;
    }

    private static List<CalendarDayV2> GetDays(JudicialCalendar calendar)
    {
        var days = new List<CalendarDayV2>();
        foreach (var item in calendar.Days)
        {
            var activities = new List<CalendarDayActivity>();
            var amActivity = item.Assignment.ActivityAm;
            var pmActivity = item.Assignment.ActivityPm;

            if (amActivity == null && pmActivity == null)
            {
                activities.Add(new CalendarDayActivity
                {
                    LocationId = item.Assignment.LocationId,
                    LocationName = item.Assignment.LocationName,
                    ActivityCode = item.Assignment.ActivityCode,
                    ActivityDisplayCode = item.Assignment.ActivityDisplayCode,
                    ActivityClassDescription = item.Assignment.ActivityClassDescription,
                    IsRemote = item.Assignment.IsVideo.GetValueOrDefault()
                });
                days.Add(new CalendarDayV2 { Date = item.Date, Activities = activities });
                continue;
            }

            if (amActivity != null && pmActivity != null && IsSameAmPmActivity(amActivity, pmActivity))
            {
                activities.Add(CreateCalendarDayActivity(amActivity));
            }
            else
            {
                if (amActivity != null)
                {
                    activities.Add(CreateCalendarDayActivity(amActivity, Period.AM));
                }

                if (pmActivity != null)
                {
                    activities.Add(CreateCalendarDayActivity(pmActivity, Period.PM));
                }
            }

            days.Add(new CalendarDayV2 { Date = item.Date, Activities = activities });
        }
        return days;

    }

    private static bool IsSameAmPmActivity(JudicialCalendarActivity amActivity, JudicialCalendarActivity pmActivity)
    {
        var sameLocation = amActivity.LocationId == pmActivity.LocationId;
        var sameActivity = amActivity.ActivityCode == pmActivity.ActivityCode;
        var sameRoom = amActivity.CourtRoomCode == pmActivity.CourtRoomCode;

        return sameLocation && sameActivity && sameRoom;
    }

    private static CalendarDayActivity CreateCalendarDayActivity(JudicialCalendarActivity activity, Period? period = null) => new()
    {
        LocationId = activity.LocationId,
        LocationName = activity.LocationName,
        ActivityCode = activity.ActivityCode,
        ActivityClassDescription = activity.ActivityClassDescription,
        ActivityDisplayCode = activity.ActivityDisplayCode,
        ActivityDescription = activity.ActivityDescription,
        IsRemote = activity.IsVideo.GetValueOrDefault(),
        RoomCode = activity.CourtRoomCode,
        Period = period
    };

}
