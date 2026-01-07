using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure.Authorization;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Models.AccessControlManagement;
using System.Threading.Tasks;

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
    [RequiresPermission(permissions: Permission.LOCK_UNLOCK_USERS)]
    public override Task<IActionResult> GetAll()
    {
        return base.GetAll();
    }

    /// <summary>
    /// Get the user information for the currently logged-in user.
    /// </summary>
    /// <returns>Active users</returns>
    [HttpGet]
    [Route("me")]
    public async Task<IActionResult> GetMyUser()
    {
        var userId = User.UserId();
        logger.LogInformation("User Id {UserId}, returning their own user information", userId);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("Invalid user. Please contact the JASPER admin.");
        }
        var user = await base.Service.GetByIdWithPermissionsAsync(User.UserId());

        if (user != null)
        {
            return Ok(user);
        }
        return NotFound("Unable to locate JASPER user. Please contact the JASPER admin.");
    }

    /// <summary>
    /// Allows a new user without authorization to JASPER to request access to the application.
    /// </summary>
    /// <returns>The user resulting from the access request.</returns>
    [HttpPut]
    [Route("request-access")]
    public async Task<IActionResult> RequestAccess()
    {
        var userId = User.UserId();
        logger.LogInformation("User Id {UserId}, requested access", userId);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("Invalid user. Please contact the JASPER admin.");
        }

        var existingUserResponse = await base.GetById(User.UserId());

        var email = User.Email();
        if (existingUserResponse is OkObjectResult okResult)
        {
            var existingUser = (UserDto)okResult.Value;
            if (existingUser != null)
            {
                if (email != existingUser.Email)
                {
                    existingUser.Email = email;
                }
                existingUser.IsPendingRegistration = true;
                var result = await base.Update(User.UserId(), existingUser);

                return result;
            }
            else
            {
                return NotFound("Unable to locate JASPER user. Please contact the JASPER admin.");
            }
        }
        else
        {
            return existingUserResponse;
        }
    }
}