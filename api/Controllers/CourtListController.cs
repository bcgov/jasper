using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Helpers.Extensions;
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
        /// <param name="judgeId">The judge id</param>
        /// <param name="proceeding">The proceeding date in the format YYYY-MM-dd</param>
        /// <returns>CourtList</returns>
        [HttpGet]
        public async Task<ActionResult<PCSSCommon.Models.ActivityClassUsage.ActivityAppearanceResultsCollection>> GetCourtList(DateTime proceeding, string agencyId = null, string roomCode = null, int? judgeId = null)
        {
            var result = (agencyId == null && roomCode == null)
                ? await _courtListService.GetJudgeCourtListAppearances(this.User.JudgeId(judgeId), proceeding)
                : await _courtListService.GetCourtListAppearances(agencyId, this.User.JudgeId(judgeId), roomCode, proceeding);

            return Ok(result);
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

            var (pdfStream, contentDisposition) = await _courtListService.GenerateReportAsync(request);

            Response.Headers.ContentDisposition = contentDisposition;

            return new FileStreamResult(pdfStream, "application/pdf");
        }
    }
}
