using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/court-locations")]
[ApiController]
public class CourtLocationsController(ICourtLocationService clService) : ControllerBase
{
    private readonly ICourtLocationService _clService = clService;

    [HttpGet]
    public async Task<ActionResult> GetCourtLocationByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Please provide a location code.");
        }

        var courtLocation = await _clService.GetCourtLocationByCodeAsync(code.Trim());

        if (!courtLocation.Succeeded)
        {
            var resultErrors = string.Join(", ", courtLocation.Errors);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to retrieve court location: {resultErrors}");
        }

        if (courtLocation.Payload == null)
        {
            return NotFound($"Court location with code '{code}' not found.");
        }

        return Ok(courtLocation.Payload);
    }
}
