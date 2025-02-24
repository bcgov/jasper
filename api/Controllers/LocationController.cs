﻿﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.Location;
using Scv.Api.Services;

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
        /// Provides Locations from all source systems (JC and PCSS).
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<List<Location>>> GetLocations()
        {
            var locations = await _locationService.GetLocations();

            return Ok(locations);
        }

        /// <summary>
        /// Provides Locations and court rooms from all source systems (JC and PCSS).
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("court-rooms")]
        public async Task<ActionResult<List<Location>>> GetLocationsAndCourtRooms()
        {
            var locations = await _locationService.GetLocationsAndCourtRooms();

            return Ok(locations);
        }
    }
}