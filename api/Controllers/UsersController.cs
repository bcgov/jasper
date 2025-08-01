using System;
using System.Collections.Generic;
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
using PCSSModels = PCSSCommon.Models;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class UsersController(
    IUserService userService,
    IValidator<UserDto> validator
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

    [HttpGet]
    [Route("judges")]
    public async Task<IActionResult> GetJudges()
    {
        if (!this.User.CanViewOthersSchedule())
        {
            return Ok(new List<PCSSModels.PersonSearchItem>());
        }

        try
        {
            var result = await this.Service.GetJudges();
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error retrieving judges.");
            Console.WriteLine(ex);
            return Ok(new List<PCSSModels.PersonSearchItem>());
        }
    }
}