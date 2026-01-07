using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Scv.Api.Infrastructure.Authorization;
using Scv.Core.Helpers.Extensions;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class ApplicationController(IConfiguration configuration) : ControllerBase
{
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Get JASPER application-specific information
    /// </summary>
    /// <returns>JASPER application information values</returns>
    [HttpGet("info")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetApplicationInfo()
    {
        return Ok(new
        {
            NutrientFeLicenseKey = _configuration.GetNonEmptyValue("NUTRIENT_FE_LICENSE_KEY"),
            Environment = _configuration.GetNonEmptyValue("ASPNETCORE_ENVIRONMENT"),
            // Include JASPER version at a later point
        });
    }
}