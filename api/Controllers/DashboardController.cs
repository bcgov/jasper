using System;
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
        /// Retrieves the schedule of the currently logged on user
        /// </summary>
        /// <param name="startDate">The start date of the schedule.</param>
        /// <param name="endDate">The end date of the schedule.</param>
        /// <param name="judgeId">The override judgeId.</param>
        /// <returns>The user schedule based on start and end dates.</returns>
        [HttpGet]
        [Route("my-schedule")]
        public async Task<IActionResult> GetMySchedule(string startDate, string endDate, int? judgeId = null)
        {
            // Only users with right KC permissions can view other's schedule
            var overrideJudgeId = judgeId != null && this.User.CanViewOthersSchedule()
                ? judgeId.GetValueOrDefault()
                : this.User.JudgeId();
            var currentDate = DateTime.Now.ToString(DashboardService.DATE_FORMAT);

            var result = await _dashboardService.GetMyScheduleAsync(overrideJudgeId, currentDate, startDate, endDate);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors });
            }

            return Ok(result);
        }
    }
}
