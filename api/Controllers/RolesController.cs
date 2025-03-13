using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Models.UserManagement;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

//[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class RolesController(
    IRoleService roleService,
    IValidator<RoleDto> validator
) : UserManagementControllerBase<IRoleService, RoleDto>(roleService, validator)
{
    /// <summary>
    /// Get all roles.
    /// </summary>
    /// <returns>List of roles.</returns>
    [HttpGet]
    public new async Task<IActionResult> GetAll()
    {
        return await base.GetAll();
    }

    /// <summary>
    /// Get role by id
    /// </summary>
    /// <param name="id">The role id.</param>
    /// <returns>Role instance</returns>
    [HttpGet("{id}")]
    public new async Task<IActionResult> GetById(string id)
    {
        return await base.GetById(id);
    }

    /// <summary>
    /// Creates a new role
    /// </summary>
    /// <param name="role">Payload to create a role</param>
    /// <returns>Created role</returns>
    [HttpPost]
    public new async Task<IActionResult> Create([FromBody] RoleDto role)
    {
        return await base.Create(role);
    }

    /// <summary>
    /// Updates an existing role
    /// </summary>
    /// <param name="id">The role id.</param>
    /// <param name="role">The role payload</param>
    /// <returns>Updated role</returns>
    [HttpPut("{id}")]
    public new async Task<IActionResult> Update(string id, [FromBody] RoleDto role)
    {
        return await base.Update(id, role);
    }

    /// <summary>
    /// Deletes a role
    /// </summary>
    /// <param name="id">Role Id in ObjectId format</param>
    /// <returns>NoContent</returns>
    [HttpDelete("{id}")]
    public new async Task<IActionResult> Delete(string id)
    {
        return await base.Delete(id);
    }
}
