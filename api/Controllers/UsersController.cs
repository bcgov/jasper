using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using Scv.Db.Models;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class UsersController(
    IUserService userService,
    IValidator<UserDto> validator,
    ILogger<UsersController> logger
) : AccessControlManagementControllerBase<IUserService, UserDto>(userService, validator)
{

    /// <summary>
    /// Get all active users
    /// </summary>
    /// <returns>Active users</returns>
    [HttpGet]
    [RequiresPermission(permissions: [Permission.LOCK_UNLOCK_USERS])]
    public override Task<IActionResult> GetAll()
    {
        return base.GetAll();
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    /// <returns>User matching email</returns>
    [HttpGet]
    [Route("email")]
    public async Task<IActionResult> GetByEmail(string email)
    {
        var matchingUser = await base.Service.GetWithPermissionsAsync(email);
        if (matchingUser != null)
        {
            return Ok(matchingUser);
        }
        return NoContent();
    }

    /// <summary>
    /// Allows a new user without authorization to JASPER to request access to the application.
    /// </summary>
    /// <returns>The user resulting from the access request.</returns>
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [HttpGet]
    [Route("request-access")]
    public async Task<ActionResult> RequestAccess(string email)
    {
        var userIdentifier = User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        logger.LogInformation("User Id {UserId}, requested access with guid: {UserIdentifier}", User.UserId(), userIdentifier);

        Guid userGuid;
        Guid.TryParse(userIdentifier?.Split("@").FirstOrDefault(), out userGuid);
        var newUser = new UserDto()
        {
            FirstName = User.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value,
            LastName = User.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value,
            Email = email,
            IsActive = false,
            IsPendingRegistration = true,
            ADId = userGuid,
            ADUsername = userIdentifier,
        };
        var result = await base.Create(newUser); // Note: this handles validation and duplicate prevention.
        return (ActionResult)result;
    }
}