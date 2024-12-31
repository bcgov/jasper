﻿using System.Collections.Generic;
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
        /// Provides locations and their court rooms. 
        /// </summary>
        /// <returns>List{Location}</returns>
        [HttpGet]
        [Route("court-rooms")]
        public async Task<ActionResult<List<Location>>> GetLocationsAndCourtRooms()
        {
            var locations = await _locationService.GetLocations();

            var locationList = locations.Select(location => new Location
            {
                Name = location.LongDesc,
                Active = location.Flex?.Equals("Y"),
                LocationId = location.ShortDesc,
                Code = location.Code
            }).ToList();

            var courtRooms = await _locationService.GetCourtRooms();

            foreach (var location in locationList)
            {
                location.CourtRooms = courtRooms.Where(cr => cr.Flex == location.LocationId && (cr.ShortDesc == "CRT" || cr.ShortDesc == "HGR"))
                    .Select(cr => new CourtRoom { LocationId = cr.Flex, Room = cr.Code, Type = cr.ShortDesc }).ToList();
            }

            return Ok(locationList);
        }


        /// <summary>
        /// Returns the list of locations used in PCSS
        /// </summary>
        /// <returns>PCSS Locations</returns>
        [HttpGet]
        [Route("pcss")]
        public async Task<ActionResult<List<Location>>> GetPCSSLocations()
        {
            var locations = await _locationService.GetPCSSLocations();

            return Ok(locations);
        }
    }
}