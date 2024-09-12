using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.Lookup;
using Scv.Api.Services;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class CodesController : ControllerBase
    {
        private readonly LookupService _lookupService;

        public CodesController(LookupService lookupService)
        {
            _lookupService = lookupService;
        }

        /// <summary>
        /// Gets the Court Classes
        /// </summary>
        /// <returns>Court Classes Lookup</returns>
        [HttpGet]
        [Route("court/classes")]
        public async Task<ActionResult<List<LookupCode>>> GetCourtClasses()
        {
            var classes = await _lookupService.GetCourtClass();

            var classesList = classes.Select(level => new LookupCode
            {
                LongDesc = level.LongDesc,
                ShortDesc = level.ShortDesc,
                Code = level.Code
            }).ToList();

            return Ok(classesList);
        }
    }
}
