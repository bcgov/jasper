using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Models.Location;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly LocationService _locationService;

        public LocationController(LocationService locationService)
        {
            _locationService = locationService;
        }

        /// <summary>
        /// Provides Locations from all source systems (JC and PCSS)
        /// </summary>
        /// <param name="includeChildRecords">Flag whether court rooms will be included or not</param>
        /// <returns>List of combined locations from JC and PCSS</returns>
        [HttpGet]
        public async Task<ActionResult<List<Location>>> GetLocations(bool includeChildRecords = false)
        {
            var locations = await _locationService.GetLocations(includeChildRecords);

            return Ok(locations);
        }
    }
}