using DARSCommon.Clients.LogNotesServices;
using DARSCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class DarsController(IDarsService darsService, ILogger<DarsController> logger) : ControllerBase
    {
        /// <summary>
        /// Search for DARS audio recordings by date, location, and court room.
        /// </summary>
        /// <param name="date">The date to search for recordings</param>
        /// <param name="agencyIdentifierCd">The location ID</param>
        /// <param name="courtRoomCd">The court room code</param>
        /// <returns>A collection of audio recording log notes</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<DarsSearchResults>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Search(DateTime date, string agencyIdentifierCd, string courtRoomCd)
        {
            var sanitizedCourtRoomCd = courtRoomCd?.Replace(Environment.NewLine, "").Trim();
            var sanitizedAgencyIdentifierCd = agencyIdentifierCd?.Replace(Environment.NewLine, "").Trim();
            logger.LogInformation(
                "DARS search requested - Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}",
                date,
                sanitizedAgencyIdentifierCd,
                sanitizedCourtRoomCd
            );

            if (string.IsNullOrWhiteSpace(sanitizedAgencyIdentifierCd))
            {
                logger.LogWarning("Invalid agencyIdentifierCd provided: {AgencyIdentifierCd}", sanitizedAgencyIdentifierCd);
                return BadRequest("agencyIdentifierCd must be non-empty.");
            }

            try
            {
                var result = await darsService.DarsApiSearch(date, sanitizedAgencyIdentifierCd, sanitizedCourtRoomCd);

                if (result == null || !result.Any())
                {
                    logger.LogInformation(
                        "No DARS recordings found for Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}",
                        date,
                        sanitizedAgencyIdentifierCd,
                        sanitizedCourtRoomCd
                    );
                    return NotFound();
                }

                logger.LogInformation(
                    "Found {Count} DARS recording(s) for Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}",
                    result.Count(),
                    date,
                    sanitizedAgencyIdentifierCd,
                    sanitizedCourtRoomCd
                );

                return Ok(result);
            }
            catch (ApiException ex)
            {
                logger.LogError(
                    ex,
                    "DARS API exception while searching - Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}, Status: {StatusCode}",
                    date,
                    sanitizedAgencyIdentifierCd,
                    sanitizedCourtRoomCd,
                    ex.StatusCode
                );

                // Return 404 for not found from upstream API
                if (ex.StatusCode == 404)
                {
                    return NotFound();
                }

                return StatusCode(500, "An error occurred while searching for audio recordings.");
            }
        }
    }
}
