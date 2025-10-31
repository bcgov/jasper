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
    public class DarsController(DarsService darsService, ILogger<DarsController> logger) : ControllerBase
    {
        /// <summary>
        /// Search for DARS audio recordings by date, location, and court room.
        /// </summary>
        /// <param name="date">The date to search for recordings</param>
        /// <param name="locationId">The location ID</param>
        /// <param name="courtRoomCd">The court room code</param>
        /// <returns>A collection of audio recording log notes</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<DarsSearchResults>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Search(DateTime date, int locationId, string courtRoomCd)
        {
            logger.LogInformation(
                "DARS search requested - Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}",
                date,
                locationId,
                courtRoomCd
            );

            // Validate input parameters
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid locationId provided: {LocationId}", locationId);
                return BadRequest("LocationId must be greater than 0.");
            }

            try
            {
                var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

                if (result == null || !result.Any())
                {
                    logger.LogInformation(
                        "No DARS recordings found for Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}",
                        date,
                        locationId,
                        courtRoomCd
                    );
                    return NotFound();
                }

                logger.LogInformation(
                    "Found {Count} DARS recording(s) for Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}",
                    result.Count(),
                    date,
                    locationId,
                    courtRoomCd
                );

                return Ok(result);
            }
            catch (ApiException ex)
            {
                logger.LogError(
                    ex,
                    "DARS API exception while searching - Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}, Status: {StatusCode}",
                    date,
                    locationId,
                    courtRoomCd,
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
