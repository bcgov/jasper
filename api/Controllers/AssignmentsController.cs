using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Api.Models.Assignment;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentsController : ControllerBase
    {
        #region Variables
        private readonly AssignmentService _assignmentService;

        #endregion Variables

        #region Constructor
        public AssignmentsController(AssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }
        #endregion Constructor

        /// <summary>
        /// Returns list of assignemnts for a given month and year for current user.
        /// </summary>
        /// <param name="year">selected year</param>
        /// <param name="month">selected month</param>
        /// <returns></returns>
       // [HttpGet("monthly-schedule/{year}/{month}")]
        [HttpGet]
        [Route("monthly-schedule/{year}/{month}")]
        public async Task<ActionResult<IEnumerable<Assignment>>> GetMonthlySchedule(int year, int month)
        {
            try
            {
				var assignments = await _assignmentService.MonthlyScheduleAsync(year, month);
                if (assignments == null)
                {
                    return Ok(new List<Assignment>());
                }
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Returns list of assignments for a given day for current user.
        /// </summary>
        /// <param name="year">selected year</param>
        /// <param name="month">selected month</param>
        /// <param name="day">selected day</param>
        /// <returns></returns>
        [HttpGet]
        [Route("daily-schedule/{year}/{month}/{day}")]
        public async Task<ActionResult<IEnumerable<Assignment>>> GetDailySchedule(int year, int month, int day)
        {
            try
            {
            var assignments = await _assignmentService.DailyScheduleAsync(year, month, day);
            if (assignments == null)
            {
                return Ok(new List<Assignment>());
            }
            return Ok(assignments);
            }
            catch (Exception ex)
            {
            // Log the exception
            return StatusCode(500, "Internal server error");
            }
        }

 
           

    }


}