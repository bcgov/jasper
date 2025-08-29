using System;
using System.ComponentModel.DataAnnotations;
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
    /// Get the user information for the currently logged-in user.
    /// </summary>
    /// <returns>Active users</returns>
    [HttpGet]
    [Route("me")]
    public Task<IActionResult> GetMyUser()
    {
        return base.GetById(User.UserId());
    }

    /// <summary>
    /// Allows a new user without authorization to JASPER to request access to the application.
    /// </summary>
    /// <returns>The user resulting from the access request.</returns>
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [HttpGet]
    [Route("request-access")]
    public async Task<IActionResult> RequestAccess(string email)
    {
        logger.LogInformation("User Id {UserId}, requested access", User.UserId());

        var existingUserResponse = (await base.GetById(User.UserId()));
        if (existingUserResponse is OkObjectResult okResult)
        {
            var existingUser = (UserDto)okResult.Value;
            if (email != existingUser?.Email)
            {
                var emailValidator = new EmailAddressAttribute();
                if (!emailValidator.IsValid(email))
                {
                    return BadRequest("Invalid email format.");
                }
                existingUser.Email = email;
            }

            var result = await base.Update(User.UserId(), existingUser);
            return result;
        } 
        else
        {
            return existingUserResponse;
        }
    }
}