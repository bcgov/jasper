using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class ReservedJudgementsController(ICrudService<ReservedJudgementDto> judgementService) : ControllerBase
{
    private readonly ICrudService<ReservedJudgementDto> _judgementService = judgementService;

    [HttpGet]
    public async Task<ActionResult> GetReservedJudgements()
    {
        var reservedJudgments = await _judgementService.GetAllAsync();

        return Ok(reservedJudgments);
    }
}