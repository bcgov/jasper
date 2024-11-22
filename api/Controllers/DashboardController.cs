using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Api.Models.Lookup;
using PCSS.Models.REST.JudicialCalendar;
using Scv.Api.Helpers;
using Scv.Api.Models.Calendar;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        #region Variables
        private readonly LocationService _locationService;
        private readonly JudicialCalendarService _judicialCalendarService;

        #endregion Variables

        #region Constructor
        public DashboardController(LocationService locationService, JudicialCalendarService judicialCalendarService)
        {
            _locationService = locationService;
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
                //  first day of the month and a week before the first day of the month
                var startDate = new DateTime(year, month, 1).AddDays(-7);
                // last day of the month and a week after the last day of the month
                var endDate = startDate.AddMonths(1).AddDays(-1).AddDays(7);
                var calendars = await _judicialCalendarService.JudicialCalendarsGetAsync(locationId, startDate, endDate);
                CalendarSchedule calendarSchedule = new CalendarSchedule();

                var calendarDays = MapperHelper.CalendarToDays(calendars.ToList());
                if (calendarDays == null)
                {
                    calendarSchedule.Schedule = new List<CalendarDay>();    
                }
                else 
                    calendarSchedule.Schedule = calendarDays;

                calendarSchedule.Presiders = calendars.Where(t => t.IsPresider).Select(presider => new FilterCode
                {
                    Text = $"{presider.RotaInitials} - {presider.Name}",
                    Value = $"{presider.ParticipantId}",
                }).DistinctBy(t => t.Value).OrderBy(x => x.Value).ToList();

            var assignmentsList = calendars.Where(t => t.IsPresider)
                                    .Where(t => t.Days?.Count > 0)
                                    .SelectMany(t => t.Days).Where(day => day.Assignment != null && (day.Assignment.ActivityAm !=null ||  day.Assignment.ActivityPm != null))
                                    .Select(day => day.Assignment)
                                    .ToList();
            var activitiesList = assignmentsList
                .SelectMany(t => new[] { t.ActivityAm, t.ActivityPm })
                .Where(activity => activity != null)
                .Select(activity => new FilterCode
                {
                    Text = activity.ActivityDescription,
                    Value = activity.ActivityCode
                })
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
                var locations = await _locationService.GetLocations();
                var locationList = locations.Where(t => t.Flex?.Equals("Y") == true).Select(location => new FilterCode
                {
                    Text = location.LongDesc,
                    Value = location.ShortDesc
                }).OrderBy(x => x.Text);

                return Ok(locationList);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "Internal server error" + ex.Message);
            }
        }
    }


}