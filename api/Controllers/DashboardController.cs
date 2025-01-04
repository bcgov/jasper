using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.Calendar;
using Scv.Api.Models.Lookup;
using Scv.Api.Services;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        #region Variables

        private readonly JudicialCalendarService _judicialCalendarService;

        #endregion Variables

        #region Constructor

        public DashboardController(JudicialCalendarService judicialCalendarService)
        {
            _judicialCalendarService = judicialCalendarService;
        }

        #endregion Constructor

        /// <summary>
        /// Retrieves the events for the specified month.
        /// </summary>
        /// <param name="year">selected year</param>
        /// <param name="month">selected month</param>
        /// <param name="locationIds">List of location ids</param>
        /// <returns></returns>
        [HttpGet]
        [Route("monthly-schedule")]
        public async Task<ActionResult<CalendarSchedule>> GetMonthlySchedule([FromQuery] int year, [FromQuery] int month, [FromQuery] string locationIds = "")
        {
            try
            {
                #region Calculate Start and End Dates of the calendar month

                // could be replaced if found on a front end in calendar properties
                var startMonthDifference = GetWeekFirstDayDifference(month, year);
                var endMonthDifference = GetLastDayOfMonthWeekDifference(month, year);
                //  first day of the month and a week before the first day of the month
                var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local).AddDays(-startMonthDifference);
                // last day of the month and a week after the last day of the month
                var endDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local).AddMonths(1).AddDays(6).AddDays(endMonthDifference);
                #endregion Calculate Start and End Dates

                CalendarSchedule calendarSchedule = new CalendarSchedule();
                var isMySchedule = string.IsNullOrWhiteSpace(locationIds);

                // Test Judge Id
                var judgeId = 190;

                // Get "Judge's Calendar" when no LocationIds provided. Otherwise, get all judge's calendar for the provided LocationIds.
                var calendars = isMySchedule
                    ? [await _judicialCalendarService.GetJudgeCalendarAsync(judgeId, startDate, endDate)]
                    : await _judicialCalendarService.JudicialCalendarsGetAsync(locationIds, startDate, endDate);

                // check if the calendar is empty and return empty schedule - do we need it at all?
                if (calendars == null)
                {
                    return Ok(calendarSchedule);
                }

                var calendarDays = MapperHelper.CalendarToDays(calendars.ToList());
                if (calendarDays == null)
                {
                    calendarSchedule.Schedule = new List<CalendarDay>();
                }
                else
                {
                    if (isMySchedule)
                        calendarDays = calendarDays.Where(t => t.Assignment != null && t.Assignment.JudgeId == judgeId).ToList();
                    calendarSchedule.Schedule = calendarDays;
                }

                calendarSchedule.Presiders = calendars
                    .Where(t => t.IsPresider && t.Days.Any())
                    .Select(presider => new FilterCode
                    {
                        Text = $"{presider.RotaInitials} - {presider.Name}",
                        Value = $"{presider.Days[0].JudgeId}",
                    })
                    .DistinctBy(t => t.Value)
                    .OrderBy(x => x.Value)
                    .ToList();

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
                Console.WriteLine(ex.ToString());

                // Log the exception
                return StatusCode(500, "Internal server error");
            }
        }

        #region Helpers

        //calcluate the difference between the first day of the month and the first day of the week for the calendar

        private static int GetWeekFirstDayDifference(int month, int year)
        {
            var firstDayOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
            return (int)firstDayOfMonth.DayOfWeek - (int)FirstDayOfWeek.Sunday + 1;
        }

        private static int GetLastDayOfMonthWeekDifference(int month, int year)
        {
            var lastDayOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local).AddMonths(1).AddDays(-1);
            int difference = (int)FirstDayOfWeek.Saturday - (int)lastDayOfMonth.DayOfWeek;
            // calendar seems to add a week if the difference is 0
            if (difference <= 0)
                difference = 7 + difference;
            // if calendar is 5 weeks we need to add a week
            var firstDayOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
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
