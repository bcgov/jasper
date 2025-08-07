using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController(IDashboardService dashboardService) : ControllerBase
    {
        private readonly IDashboardService _dashboardService = dashboardService;

        /// <summary>
        /// Retrieves the currently logged on judge's court activities for today
        /// </summary>
        /// <param name="judgeId">The override judgeId.</param>
        /// <returns>Judge's schedule for today.</returns>
        [HttpGet]
        [Route("today")]
        public async Task<IActionResult> GetTodaysSchedule(int? judgeId = null)
        {
            var result = await _dashboardService.GetTodaysScheduleAsync(this.User.JudgeId(judgeId));
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors });
            }

            return Ok(result);
        }

        /// <summary>
        /// Retrieves the schedule of the currently logged on user
        /// </summary>
        /// <param name="startDate">The start date of the schedule.</param>
        /// <param name="endDate">The end date of the schedule.</param>
        /// <param name="judgeId">The override judgeId.</param>
        /// <returns>The user's schedule based on start and end dates.</returns>
        [HttpGet]
        [Route("my-schedule")]
        public async Task<IActionResult> GetMySchedule(string startDate, string endDate, int? judgeId = null)
        {
            var result = await _dashboardService.GetMyScheduleAsync(this.User.JudgeId(judgeId), startDate, endDate);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors });
            }

            return Ok(result);
        }

        /// <summary>
        /// Retrieves the court calendar based on the given location id(s), start and end dates.
        /// </summary>
        /// <param name="startDate">The start date of the schedule.</param>
        /// <param name="endDate">The end date of the schedule.</param>
        /// <param name="locationIds">List location ids.</param>
        /// <returns>Court calendar</returns>
        [HttpGet]
        [Route("court-calendar")]
        public async Task<IActionResult> GetCourtCalendar(string startDate, string endDate, string locationIds = "")
        {
            var ids = string.IsNullOrWhiteSpace(locationIds) ? this.User.JudgeHomeLocationId().ToString() : locationIds;

            var result = await _dashboardService.GetCourtCalendarScheduleAsync(ids, startDate, endDate);

            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors });
            }

            return Ok(result);
        }
    }
}
