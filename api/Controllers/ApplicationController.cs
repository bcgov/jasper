using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models;
using Scv.Api.Processors;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class ApplicationController(
    IConfiguration configuration,
    IConfigurationService configurationService,
    IValidator<AppDocumentUploadRequest> appDocumentUploadRequestValidator,
    IFileUploadProcessor fileUploadProcessor,
    IAppDocumentService appDocumentService) : ControllerBase
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IConfigurationService _configurationService = configurationService;
    private readonly IValidator<AppDocumentUploadRequest> _appDocumentUploadRequestValidator = appDocumentUploadRequestValidator;
    private readonly IFileUploadProcessor _fileUploadProcessor = fileUploadProcessor;
    private readonly IAppDocumentService _appDocumentService = appDocumentService;

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
        var useCourtLocations = _configuration?.GetValue<bool>("USE_COURT_LOCATIONS") ?? false;

        return Ok(new
        {
            Version = version,
            NutrientFeLicenseKey = _configuration.GetNonEmptyValue("NUTRIENT_FE_LICENSE_KEY"),
            Environment = _configuration.GetNonEmptyValue("ASPNETCORE_ENVIRONMENT"),
            Configuration = dbConfig,
            UseCourtLocations = useCourtLocations
        });
    }

    /// <summary>
    /// Uploads the app document file with the specified key. Default app document records are seeded in the databse and this  allows for updating the real file content.
    /// </summary>
    /// <param name="request">The upload request containing the key and the file.</param>
    /// <returns>The saved app document without its data.</returns>
    [HttpPut("documents")]
    [RequiresPermission(permissions: Permission.LOCK_UNLOCK_USERS)]
    public async Task<IActionResult> UploadAppDocument([FromForm] AppDocumentUploadRequest request)
    {
        var upload = await _fileUploadProcessor.ValidateAndScanAsync(request, _appDocumentUploadRequestValidator);
        if (!upload.Succeeded)
        {
            return BadRequest(upload.Errors[0]);
        }

        var result = await _appDocumentService.UpdateAsync(request.Key, upload.Payload);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found"))
                ? NotFound(new { error = result.Errors })
                : StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Errors });
        }

        return Ok(result.Payload);
    }
}
