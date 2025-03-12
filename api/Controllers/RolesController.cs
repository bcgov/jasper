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
public class RolesController(
    IRoleService roleService,
    IValidator<RoleCreateDto> createValidator,
    IValidator<RoleUpdateDto> updateValidator
) : ControllerBase
{
    private readonly IRoleService _roleService = roleService;
    private readonly IValidator<RoleCreateDto> _createValidator = createValidator;
    private readonly IValidator<RoleUpdateDto> _updateValidator = updateValidator;

    /// <summary>
    /// Get roles
    /// </summary>
    /// <returns>List of roles</returns>
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        return Ok(await _roleService.GetRolesAsync());
    }

    /// <summary>
    /// Get role by id
    /// </summary>
    /// <param name="id">Object ID of the role</param>
    /// <returns>Role</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoleById(string id)
    {
        if (!ObjectId.TryParse(id.ToString(), out _))
        {
            return BadRequest("Invalid ID.");
        }

        var role = await _roleService.GetRoleByIdAsync(id);

        if (role == null)
        {
            return NotFound();
        }

        return Ok(role);
    }

    /// <summary>
    /// Creates a new role
    /// </summary>
    /// <param name="role">Payload to create a role</param>
    /// <returns>Created role</returns>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] RoleCreateDto role)
    {
        var basicValidation = await _createValidator.ValidateAsync(role);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessRulesValidation = await _roleService.ValidateRoleCreateDtoAsync(role);
        if (!businessRulesValidation.Succeeded)
        {
            return BadRequest(new { error = businessRulesValidation.Errors });
        }

        var result = await _roleService.CreateRoleAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return CreatedAtAction(nameof(GetRoleById), new { id = result.Payload.Id }, result.Payload);
    }

    /// <summary>
    /// Updates an existing role
    /// </summary>
    /// <param name="id">Role Id in ObjectId format</param>
    /// <param name="role">Role payload</param>
    /// <returns>Updated role</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleUpdateDto role)
    {
        var context = new ValidationContext<RoleUpdateDto>(role)
        {
            RootContextData = { ["RouteId"] = id }
        };

        var basicValidation = await _updateValidator.ValidateAsync(context);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessRulesValidation = await _roleService.ValidateRoleUpdateDtoAsync(role);
        if (!businessRulesValidation.Succeeded)
        {
            return BadRequest(new { error = businessRulesValidation.Errors });
        }

        var result = await _roleService.UpdateRoleAsync(id, role);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return Ok(result.Payload);
    }

    /// <summary>
    /// Deletes a role
    /// </summary>
    /// <param name="id">Role Id in ObjectId format</param>
    /// <returns>NoContent</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest("Invalid ID.");
        }

        var result = await _roleService.DeleteRoleAsync(id);
        if (!result.Succeeded)
        {

            return BadRequest(new { errors = result.Errors });
        }

        return NoContent();
    }
}
