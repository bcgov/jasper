using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Api.Models.Lookup;
using Scv.Api.Helpers;
using Scv.Api.Models.Calendar;
using Microsoft.VisualBasic;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        #region Variables
        private readonly LocationPCSSService _pcssLocationService;
        private readonly JudicialCalendarService _judicialCalendarService;

        #endregion Variables

        #region Constructor
        public DashboardController(LocationPCSSService pcssLocationService, JudicialCalendarService judicialCalendarService)
        {
            _pcssLocationService = pcssLocationService;
            _judicialCalendarService = judicialCalendarService;
        }
        #endregion Constructor

        /// <summary>
        /// Returns list of assignemnts for a given month and year for current user.
        /// </summary>
        /// <param name="year">selected year</param>
        /// <param name="month">selected month</param>
        /// <param name="locationId">selected month</param>
        /// <returns></returns>
       // [HttpGet("monthly-schedule/{year}/{month}")]
        [HttpGet]
        [Route("monthly-schedule/{year}/{month}")]
        public async Task<ActionResult<CalendarSchedule>> GetMonthlySchedule(int year, int month, [FromQuery] string locationId = "")
        {
            try
            {
                #region Calculate Start and End Dates of the calendar month

                // could be replaced if found on a front end in calendar properties
                var startMonthDifference = GetWeekFirstDayDifference(month, year);
                var endMonthDifference = GetLastDayOfMonthWeekDifference(month, year);
                //  first day of the month and a week before the first day of the month
                var startDate = new DateTime(year, month, 1).AddDays(-startMonthDifference);
                // last day of the month and a week after the last day of the month
                var endDate = new DateTime(year, month + 1, 1).AddDays(6).AddDays(endMonthDifference);
                #endregion Calculate Start and End Dates

                CalendarSchedule calendarSchedule = new CalendarSchedule();
                var isMySchedule = string.IsNullOrWhiteSpace(locationId);

                // hard coded location for "my schedule", judgeId hardcoded below
                // both endpoints do not work, we should use them instead
                //	<baseURL>/api/v2/calendar/judges/190/
                //	<baseURL>/api/v2/calendar/judges?judgeId=12&startDate=22-Feb-2019&endDate=28-Mar-2019
                var calendars = (isMySchedule) ? await _judicialCalendarService.JudicialCalendarsGetAsync("2", startDate, endDate)
                : await _judicialCalendarService.JudicialCalendarsGetAsync(locationId, startDate, endDate);
                // the call for my schedule could be replaced with the another service if needed

                // check if the calendar is empty and return empty schedule - do we need it at all?
                // if (calendars == null)
                // {
                //   return Ok(calendarSchedule);
                //}

                var calendarDays = MapperHelper.CalendarToDays(calendars.ToList());
                if (calendarDays == null)
                {
                    calendarSchedule.Schedule = new List<CalendarDay>();
                }
                else
                {
                    if (isMySchedule)
                        calendarDays = calendarDays.Where(t => t.Assignment != null && t.Assignment.JudgeId == 12).ToList();
                    calendarSchedule.Schedule = calendarDays;
                }

                calendarSchedule.Presiders = calendars.Where(t => t.IsPresider && t.Days != null).Select(presider => new FilterCode
                {
                    Text = $"{presider.RotaInitials} - {presider.Name}",
                    Value = $"{presider.Days[0].JudgeId}",
                }).DistinctBy(t => t.Value).OrderBy(x => x.Value).ToList();

                // check if it should isJudge or IsPresider
                var assignmentsListFull = calendars.Where(t => t.IsPresider)
                                        .Where(t => t.Days?.Count > 0)
                                        .SelectMany(t => t.Days).Where(day => day.Assignment != null)
                                        .Select(day => day.Assignment)
                                        .ToList();

                var activitiesList = assignmentsListFull
                .Where(activity => activity != null && activity.ActivityCode != null && activity.ActivityDescription != null)
                .Select(activity => new FilterCode
                {
                    Text = activity.ActivityDescription,
                    Value = activity.ActivityCode
                }).ToList();


                // merging activities information form activityAm and activityPm, and assignmentsListFull
                var assignmentsList = calendars.Where(t => t.IsPresider)
                                        .Where(t => t.Days?.Count > 0)
                                        .SelectMany(t => t.Days).Where(day => day.Assignment != null && (day.Assignment.ActivityAm != null || day.Assignment.ActivityPm != null))
                                        .Select(day => day.Assignment)
                                        .ToList();

                activitiesList.AddRange(assignmentsList
                   .SelectMany(t => new[] { t.ActivityAm, t.ActivityPm })
                   .Where(activity => activity != null && activity.ActivityCode != null && activity.ActivityDescription != null)
                   .Select(activity => new FilterCode
                   {
                       Text = activity.ActivityDescription,
                       Value = activity.ActivityCode
                   }));

                activitiesList = activitiesList
                .DistinctBy(t => t.Value)
                .OrderBy(x => x.Text)
                .ToList();
                calendarSchedule.Activities = activitiesList;

                return Ok(calendarSchedule);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "Internal server error");
            }
        }

        //public async Task<ActionResult<List<FilterCode>>> LocationList(int a)
        /// <summary>
        /// Provides locations.
        /// </summary>
        /// <returns>IEnumerable{FilterCode}</returns>
        [HttpGet]
        [Route("locations")]
        public async Task<ActionResult<IEnumerable<FilterCode>>> LocationList()
        {
            try
            {
                var locations = await _pcssLocationService.PCSSLocationsGetAsync();

                var locationList = locations
                    .Where(location => (location.ActiveYn?.Equals("Y") ?? false))
                    .Select(location => new FilterCode
                    {
                        Text = location.LocationNm,
                        Value = location.LocationId.ToString()
                    })
                    .OrderBy(x => x.Value)
                    .ToList();

                return Ok(locationList);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "Internal server error" + ex.Message);
            }
        }

        #region Helpers
        //calcluate the difference between the first day of the month and the first day of the week for the calendar
        public int GetWeekFirstDayDifference(int month, int year)
        {
            var firstDayOfMonth = new DateTime(year, month, 1);
            return (int)firstDayOfMonth.DayOfWeek - (int)FirstDayOfWeek.Sunday + 1;
        }
        public int GetLastDayOfMonthWeekDifference(int month, int year)
        {
            var lastDayOfMonth = new DateTime(year, month + 1, 1).AddDays(-1);
            int difference = (int)FirstDayOfWeek.Saturday - (int)lastDayOfMonth.DayOfWeek;
            // calendar seems to add a week if the difference is 0
            if (difference <= 0)
                difference = 7 + difference;
            // if calendar is 5 weeks we need to add a week
            var firstDayOfMonth = new DateTime(year, month, 1);
            var totalDays = (lastDayOfMonth - firstDayOfMonth).Days + 1;
            var fullWeeks = totalDays / 7;
            if (totalDays % 7 > 0)
            {
                fullWeeks++;
            }
            if (fullWeeks == 5)
                _ = difference + 7;


            return difference;
        }
        #endregion Helpers
    }


}
