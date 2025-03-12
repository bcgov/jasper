using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.UserManagement;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class PermissionsController(
    IPermissionService permissionService,
    IValidator<PermissionUpdateDto> updateValidator
) : ControllerBase
{
    private readonly IPermissionService _permissionService = permissionService;
    private readonly IValidator<PermissionUpdateDto> _updateValidator = updateValidator;

    /// <summary>
    /// Gets the active permissions
    /// </summary>
    /// <returns>Active permissions</returns>
    [HttpGet]
    public async Task<IActionResult> GetPermissions()
    {
        return Ok(await _permissionService.GetPermissionsAsync());
    }

    /// <summary>
    /// Gets permission by id
    /// </summary>
    /// <returns>Active permission</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPermissionById(string id)
    {
        if (!ObjectId.TryParse(id.ToString(), out _))
        {
            return BadRequest("Invalid ID.");
        }

        var permission = await _permissionService.GetPermissionByIdAsync(id);

        if (permission == null)
        {
            return NotFound();
        }

        return Ok(permission);
    }

    /// <summary>
    /// Updates the permission details
    /// </summary>
    /// <param name="id">Permission Id</param>
    /// <param name="permission">Payload to update permission</param>
    /// <returns>Updated permission</returns>
    [HttpPut]
    public async Task<IActionResult> UpdatePermission(string id, [FromBody] PermissionUpdateDto permission)
    {
        var context = new ValidationContext<PermissionUpdateDto>(permission)
        {
            RootContextData = { ["RouteId"] = id }
        };

        var basicValidation = await _updateValidator.ValidateAsync(context);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessRulesValidation = await _permissionService.ValidatePermissionUpdateDtoAsync(permission);
        if (!businessRulesValidation.Succeeded)
        {
            return BadRequest(new { error = businessRulesValidation.Errors });
        }

        var result = await _permissionService.UpdatePermissionAsync(id, permission);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return Ok(result.Payload);
    }
}
