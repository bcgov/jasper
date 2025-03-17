using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class UsersController(
    IAccessControlManagementService<UserDto> userService,
    IValidator<UserDto> validator
) : AccessControlManagementControllerBase<IAccessControlManagementService<UserDto>, UserDto>(userService, validator)
{
}