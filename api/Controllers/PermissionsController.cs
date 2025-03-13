using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.UserManagement;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

//[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class PermissionsController(
    IPermissionService permissionService,
    IValidator<PermissionDto> validator
) : UserManagementControllerBase<IPermissionService, PermissionDto>(permissionService, validator)
{
    /// <summary>
    /// Get all permissions.
    /// </summary>
    /// <returns>List of permissions.</returns>
    [HttpGet]
    public new async Task<IActionResult> GetAll()
    {
        return await base.GetAll();
    }

    /// <summary>
    /// Get permission by id
    /// </summary>
    /// <param name="id">The permission id.</param>
    /// <returns>permission instance</returns>
    [HttpGet("{id}")]
    public new async Task<IActionResult> GetById(string id)
    {
        return await base.GetById(id);
    }

    /// <summary>
    /// Updates an existing permission
    /// </summary>
    /// <param name="id">The permission id.</param>
    /// <param name="permission">The permission payload</param>
    /// <returns>Updated permission</returns>
    [HttpPut("{id}")]
    public new async Task<IActionResult> Update(string id, [FromBody] PermissionDto permission)
    {
        return await base.Update(id, permission);
    }
}
