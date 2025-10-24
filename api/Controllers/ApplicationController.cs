using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Authorization;

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