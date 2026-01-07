using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using System.Threading.Tasks;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class ApplicationController(IConfiguration configuration, IConfigurationService configurationService) : ControllerBase
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IConfigurationService _configurationService = configurationService;

    /// <summary>
    /// Get JASPER application-specific information
    /// </summary>
    /// <returns>JASPER application information values</returns>
    [HttpGet("info")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicationInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.
            InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "Unknown";
        var dbConfig = await _configurationService.GetConfigurationAsync();

        return Ok(new
        {
            Version = version,
            NutrientFeLicenseKey = _configuration.GetNonEmptyValue("NUTRIENT_FE_LICENSE_KEY"),
            Environment = _configuration.GetNonEmptyValue("ASPNETCORE_ENVIRONMENT"),
            Configuration = dbConfig,
        });
    }
}