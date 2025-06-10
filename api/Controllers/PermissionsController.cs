using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.AccessControlManagement;

using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class PermissionsController(
    ICrudService<PermissionDto> permissionService,
    IValidator<PermissionDto> validator
) : AccessControlManagementControllerBase<ICrudService<PermissionDto>, PermissionDto>(permissionService, validator)
{
    /// <summary>
    /// Creating permission is not allowed
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>Status405MethodNotAllowed</returns>
    [NonAction]
    public override Task<IActionResult> Create(PermissionDto dto)
    {
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status405MethodNotAllowed));
    }

    /// <summary>
    /// Deleting permission is not allowed
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Status405MethodNotAllowed</returns>
    [NonAction]
    public override Task<IActionResult> Delete(string id)
    {
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status405MethodNotAllowed));
    }
}