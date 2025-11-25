using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/quick-links")]
[ApiController]
public class QuickLinksController(IQuickLinkService quickLinkService) : ControllerBase
{
    private readonly IQuickLinkService _quickLinkService = quickLinkService;

    [HttpGet]
    public async Task<IActionResult> GetQuickLinks()
    {
        var result = await _quickLinkService.GetJudgeQuickLinks();
        return Ok(result);
    }
}