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
        if (judgeCases.Succeeded)
            judgeCases.Payload.DueDateEditingJudgeId = await GetDueDateEditingJudgeIdAsync();

        return Ok(judgeCases);
    }

    [HttpPut("{id}/due-date-override")]
    [RequiresPermission(permissions: Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE)]
    public async Task<ActionResult> SetDueDateOverride(string id, [FromBody] CaseDueDateOverrideRequest request)
    {
        var judgeId = await GetDueDateEditingJudgeIdAsync();
        if (!judgeId.HasValue) return Forbid();

        var (status, result) = await _caseService.SetDueDateOverrideAsync(id, judgeId.Value, request);
        return StatusCode(status, result);
    }

    private async Task<int?> GetDueDateEditingJudgeIdAsync()
    {
        if (User.Identity?.IsAuthenticated != true
            || !User.IsActive()
            || !User.HasPermissions([Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE])) return null;

        // JudgeId() can contain a configured viewing fallback. A write needs a real association.
        var userId = User.UserId();
        var mappedJudgeId = string.IsNullOrWhiteSpace(userId)
            ? null : (await userService.GetByIdWithPermissionsAsync(userId))?.JudgeId;
        var judgeId = mappedJudgeId ?? (int.TryParse(User.ExternalJudgeId(), out var externalId) ? externalId : 0);
        return judgeId > 0 && judgeId == User.JudgeId() ? judgeId : null;
    }
}
