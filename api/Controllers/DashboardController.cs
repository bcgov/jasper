using System;
using System.Threading.Tasks;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController(JudicialCalendarService judicialCalendarService, IMapper mapper, IDashboardService dashboardService) : ControllerBase
    {
        #region Variables

        private readonly JudicialCalendarService _judicialCalendarService = judicialCalendarService;
        private readonly IMapper _mapper = mapper;
        private readonly IDashboardService _dashboardService = dashboardService;

        #endregion Variables

        #region Methods

        /// <summary>
        /// Retrieves the schedule of the currently logged on user
        /// </summary>
        /// <param name="startDate">The start date of the schedule.</param>
        /// <param name="endDate">The end date of the schedule.</param>
        /// <returns>The user schedule based on start and end dates.</returns>
        [HttpGet]
        [Route("my-schedule")]
        public async Task<IActionResult> GetMySchedule(string startDate, string endDate)
        {
            var currentDate = DateTime.Now.ToString(DashboardService.DATE_FORMAT);

            var result = await _dashboardService.GetMyScheduleAsync(this.User.JudgeId(), currentDate, startDate, endDate);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors });
            }

            return Ok(result);
        }

        #endregion Methods

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
