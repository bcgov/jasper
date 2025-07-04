using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using PCSSCommon.Clients.JudicialCalendarServices;
using PCSSCommon.Clients.SearchDateServices;
using Scv.Api.Infrastructure;
using Scv.Api.Models.Calendar;

using PCSS = PCSSCommon.Models;

namespace Scv.Api.Services;

public interface IDashboardService
{
    Task<OperationResult<CalendarSchedule>> GetMyScheduleAsync(int judgeId, string currentDate, string startDate, string endDate);
}

public class DashboardService(
    IAppCache cache,
    JudicialCalendarServicesClient calendarClient,
    SearchDateClient searchDateClient,
    LocationService locationService,
    IMapper mapper) : ServiceBase(cache), IDashboardService
{
    public const string DATE_FORMAT = "dd-MMM-yyyy";
    public const string SITTING_ACTIVITY_CODE = "SIT";
    public const string NON_SITTING_ACTIVITY_CODE = "NS";

    private readonly JudicialCalendarServicesClient _calendarClient = calendarClient;
    private readonly SearchDateClient _searchDateClient = searchDateClient;
    private readonly LocationService _locationService = locationService;
    private readonly IMapper _mapper = mapper;

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

        async Task<PCSS.JudicialCalendar> MySchedule() => await _calendarClient.ReadCalendarV2Async(judgeId, formattedStartDate, formattedEndDate);

        var myScheduleTask = this.GetDataFromCache($"{this.CacheName}-{judgeId}-{formattedStartDate}-{formattedEndDate}", MySchedule);

        var mySchedule = await myScheduleTask;

        var days = await GetDays(mySchedule);


        // Determine if today's schedule needs to be queried separately
        var isInRange = validCurrentDate >= validStartDate && validCurrentDate <= validEndDate;
        if (isInRange)
        {
            return OperationResult<CalendarSchedule>.Success(new CalendarSchedule
            {
                Days = days,
                Today = await GetTodaysSchedule(judgeId, formattedCurrentDate, days)
            });
        }

        // Query schedule for today
        async Task<PCSS.JudicialCalendar> TodaysSchedule() => await _calendarClient.ReadCalendarV2Async(judgeId, formattedCurrentDate, formattedCurrentDate);

        var todayScheduleTask = this.GetDataFromCache($"{this.CacheName}-{judgeId}-{formattedCurrentDate}-{formattedCurrentDate}", TodaysSchedule);

        var todaySchedule = await todayScheduleTask;

        var today = await GetDays(todaySchedule);

        return OperationResult<CalendarSchedule>.Success(new CalendarSchedule
        {
            Days = days,
            Today = await GetTodaysSchedule(judgeId, formattedCurrentDate, today)
        });

    }

    private async Task<CalendarDay> GetTodaysSchedule(int judgeId, string currentDate, List<CalendarDay> days)
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

    private async Task<List<CalendarDay>> GetDays(PCSS.JudicialCalendar calendar)
    {
        var days = new List<CalendarDay>();
        foreach (var day in calendar.Days)
        {
            var activities = await GetDayActivities(day);

            DateTime.TryParseExact(day.Date, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
            var isWeekend = date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday;
            var showCourtList = activities
                .Any(a => a.ActivityClassCode != SITTING_ACTIVITY_CODE
                    && a.ActivityClassCode != NON_SITTING_ACTIVITY_CODE);

            days.Add(new CalendarDay
            {
                Date = day.Date,
                IsWeekend = isWeekend,
                ShowCourtList = showCourtList,
                Activities = activities
            });

        }
        return days;
    }

    private async Task<List<CalendarDayActivity>> GetDayActivities(PCSS.JudicialCalendarDay day)
    {
        var activities = new List<CalendarDayActivity>();
        var amActivity = day.Assignment.ActivityAm;
        var pmActivity = day.Assignment.ActivityPm;


        if (amActivity == null && pmActivity == null)
        {
            activities.Add(await CreateCalendarDayActivity(day.Assignment, day.Restrictions));
            return activities;
        }

        if (amActivity != null && pmActivity != null && IsSameAmPmActivity(amActivity, pmActivity))
        {
            activities.Add(await CreateCalendarDayActivity(amActivity, day.Restrictions));
            return activities;
        }

        if (amActivity != null)
        {
            activities.Add(await CreateCalendarDayActivity(amActivity, day.Restrictions, Period.AM));
        }

        if (pmActivity != null)
        {
            activities.Add(await CreateCalendarDayActivity(pmActivity, day.Restrictions, Period.PM));
        }

        return activities;
    }

    private static bool IsSameAmPmActivity(PCSS.JudicialCalendarActivity amActivity, PCSS.JudicialCalendarActivity pmActivity)
    {
        var sameLocation = amActivity.LocationId == pmActivity.LocationId;
        var sameActivity = amActivity.ActivityCode == pmActivity.ActivityCode;
        var sameRoom = amActivity.CourtRoomCode == pmActivity.CourtRoomCode;

        return sameLocation && sameActivity && sameRoom;
    }

    private async Task<CalendarDayActivity> CreateCalendarDayActivity(
        PCSS.JudicialCalendarActivity judicialActivity,
        List<PCSS.AdjudicatorRestriction> judicialRestrictions,
        Period? period = null)
    {
        var activity = _mapper.Map<CalendarDayActivity>(judicialActivity);
        var restrictions = _mapper.Map<List<AdjudicatorRestriction>>(judicialRestrictions.Where(r => r.ActivityCode == activity.ActivityCode));

        activity.LocationShortName = activity.LocationId != null
            ? await _locationService.GetLocationShortName(activity.LocationId.ToString())
            : null;
        activity.Period = period;
        activity.Restrictions = restrictions;

        return activity;
    }

    private async Task<CalendarDayActivity> CreateCalendarDayActivity(
        PCSS.JudicialCalendarAssignment assignment,
        List<PCSS.AdjudicatorRestriction> judicialRestrictions)
    {
        var activity = _mapper.Map<CalendarDayActivity>(assignment);
        var restrictions = _mapper.Map<List<AdjudicatorRestriction>>(judicialRestrictions.Where(r => r.ActivityCode == activity.ActivityCode));

        activity.LocationShortName = assignment.LocationId != null
                    ? await _locationService.GetLocationShortName(assignment.LocationId.ToString())
                    : null;
        activity.Restrictions = restrictions;

        return activity;
    }

}
