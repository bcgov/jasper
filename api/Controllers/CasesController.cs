using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using System.Threading.Tasks;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class CasesController(ICaseService caseService) : ControllerBase
{
    private readonly ICaseService _caseService = caseService;

    [HttpGet]
    public async Task<ActionResult> GetAssignedCases(int? judgeId = null)
    {
        var judgeCases = await _caseService.GetAssignedCasesAsync(this.User.JudgeId(judgeId));

        return Ok(judgeCases);
    }
}