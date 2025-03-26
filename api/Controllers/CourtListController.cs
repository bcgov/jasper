using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.CourtList;
using Scv.Api.Services;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]

    [ApiController]
    public class CourtListController(CourtListService courtListService, IValidator<CourtListReportRequest> reportValidator) : ControllerBase
    {
        #region Variables

        private readonly CourtListService _courtListService = courtListService;
        private readonly IValidator<CourtListReportRequest> _reportValidator = reportValidator;

        #endregion Variables
        #region Constructor

        #endregion Constructor

        /// <summary>
        /// Gets a court list.
        /// </summary>
        /// <param name="agencyId">Agency Identifier Code (Location Code)</param>
        /// <param name="roomCode">The room code</param>
        /// <param name="proceeding">The proceeding date in the format YYYY-MM-dd</param>
        /// <returns>CourtList</returns>
        [HttpGet]
        [Route("court-list")]
        public async Task<ActionResult<PCSSCommon.Models.ActivityClassUsage.ActivityAppearanceResultsCollection>> GetCourtList(string agencyId, string roomCode, DateTime proceeding)
        {
            const int TEST_JUDGE_ID = 190;

            var courtList = await _courtListService.GetCourtListAppearances(agencyId, TEST_JUDGE_ID, roomCode, proceeding);

            return Ok(courtList);
        }

        /// <summary>
        /// Generates a Court List PDF report.
        /// </summary>
        /// <param name="request">Criteria to generate the pdf report</param>
        /// <returns>PDF</returns>
        [HttpGet]
        [Route("generate-report")]
        public async Task<IActionResult> GenerateReport([FromQuery] CourtListReportRequest request)
        {
            var validationResult = await _reportValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var pdfStream = await _courtListService.GenerateReportAsync(request);

            return new FileStreamResult(pdfStream, "application/pdf");
        }
    }
}
