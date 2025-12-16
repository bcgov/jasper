using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Scv.Api.Documents;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class BindersController(IBinderService binderService) : ControllerBase
{
    private readonly IBinderService _binderService = binderService;

    [HttpGet]
    public async Task<IActionResult> GetBinders()
    {
        var labels = this.Request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        var result = await _binderService.GetByLabels(labels);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBinder(BinderDto dto)
    {
        var result = await _binderService.AddAsync(dto);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        var routeValues = new RouteValueDictionary(result.Payload.Labels);

        return CreatedAtAction(nameof(GetBinders), routeValues, result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateBinder(BinderDto dto)
    {
        var result = await _binderService.UpdateAsync(dto);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> DeleteBinder(string id)
    {
        var result = await _binderService.DeleteAsync(id);
        if (!result.Succeeded)
        {

            return BadRequest(new { error = result.Errors });
        }

        return NoContent();
    }

    [HttpPost]
    [Route("bundle")]
    public async Task<IActionResult> CreateDocumentBundle([FromBody] List<Dictionary<string, string>> request, [FromQuery] Dictionary<string, List<string>> filters = null)
    {
        if (request == null || request.Count == 0)
        {
            return BadRequest("Invalid request.");
        }

        var result = await _binderService.CreateDocumentBundle(request, filters);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }
        return Ok(result);
    }
}
