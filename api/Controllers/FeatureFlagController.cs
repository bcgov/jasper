using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Scv.Api.Infrastructure.Authorization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class FeatureFlagController(IFeatureManagerSnapshot featureManager) : ControllerBase
{
    private readonly IFeatureManagerSnapshot _featureManager = featureManager;

    [HttpGet]
    public async Task<ActionResult<IDictionary<string, bool>>> GetFeatureFlags(CancellationToken cancellationToken = default)
    {
        var flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        await foreach (var feature in _featureManager.GetFeatureNamesAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            flags[feature] = await _featureManager.IsEnabledAsync(feature);
        }

        return Ok(flags);
    }
}
