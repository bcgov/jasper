using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Models;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class CasesController(ICaseService caseService, IUserService userService) : ControllerBase
{
    private readonly ICaseService _caseService = caseService;

    [HttpGet]
    public async Task<ActionResult> GetAssignedCases(int? judgeId = null)
    {
        var judgeCases = await _caseService.GetAssignedCasesAsync(this.User.JudgeId(judgeId));

        return Ok(judgeCases);
    }

    [HttpPut("{id}/due-date-override")]
    public async Task<ActionResult> SetDueDateOverride(string id, [FromBody] CaseDueDateOverrideRequest request)
    {
        if (!User.HasRoles([Role.JUDGE, Role.ACJ_CHIEF_JUDGE, Role.RAJ], applyOrCondition: true))
            return Forbid();

        // JudgeId() can contain a configured viewing fallback. A write needs a real association.
        var userId = User.UserId();
        var mappedJudgeId = string.IsNullOrWhiteSpace(userId)
            ? null : (await userService.GetByIdWithPermissionsAsync(userId))?.JudgeId;
        var judgeId = mappedJudgeId ?? (int.TryParse(User.ExternalJudgeId(), out var externalId) ? externalId : 0);
        if (judgeId <= 0 || judgeId != User.JudgeId()) return Forbid();

        var (status, result) = await _caseService.SetDueDateOverrideAsync(id, judgeId, request);
        return StatusCode(status, result);
    }
}
