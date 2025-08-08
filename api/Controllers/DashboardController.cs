using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using PCSSModels = PCSSCommon.Models;

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
            var currentDate = DateTime.Now.ToString(DashboardService.DATE_FORMAT);

            var result = await _dashboardService.GetMyScheduleAsync(this.User.JudgeId(judgeId), currentDate, startDate, endDate);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors });
            }

            return Ok(result);
        }

        [HttpGet]
        [Route("judges")]
        public async Task<IActionResult> GetJudges()
        {
            if (!this.User.CanViewOthersSchedule())
            {
                return Ok(new List<PCSSModels.PersonSearchItem>());
            }

            try
            {
                var result = await _dashboardService.GetJudges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving judges.");
                Console.WriteLine(ex);
                return Ok(new List<PCSSModels.PersonSearchItem>());
            }
        }
    }
}
