using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.UserManagement;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class PermissionsController(IPermissionService permissionService) : ControllerBase
{
    private readonly IPermissionService _permissionService = permissionService;

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
    /// <param name="permission">Payload to update permission (description and isActive) only</param>
    /// <returns>Updated permission</returns>
    [HttpPut]
    public async Task<IActionResult> UpdatePermission(string id, [FromBody] PermissionUpdateDto permission)
    {
        if (!this.ModelState.IsValid)
        {
            return BadRequest(this.ModelState);
        }

        var existingPermission = await _permissionService.GetPermissionByIdAsync(id);
        if (existingPermission == null)
        {
            return NotFound();
        }

        return Ok(await _permissionService.UpdatePermissionAsync(id, permission));
    }
}
