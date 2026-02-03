using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class JudgesController(IJudgeService judgeService) : ControllerBase
{
    private readonly IJudgeService _judgeService = judgeService;

    /// <summary>
    /// Retrieves the list of active judge. This list only includes the following judge positions: CJ, ACJ, RAJ, PJ and SJ.
    /// </summary>
    /// <returns>List of active judges.</returns>
    [HttpGet]
    public async Task<IActionResult> GetJudges()
    {
        if (!this.User.CanViewOthersSchedule())
        {
            return Unauthorized();
        }

        var positionCodes = new List<string>
        {
            JudgeService.CHIEF_JUDGE,
            JudgeService.ASSOC_CHIEF_JUDGE,
            JudgeService.REGIONAL_ADMIN_JUDGE,
            JudgeService.PUISNE_JUDGE,
            JudgeService.SENIOR_JUDGE
        };
        var result = await _judgeService.GetJudges(positionCodes);
        return Ok(result);
    }
}
