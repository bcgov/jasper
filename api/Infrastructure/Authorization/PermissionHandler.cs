using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Services;

namespace Scv.Api.Infrastructure.Authorization;

public class PermissionHandler(
    ILogger<PermissionHandler> logger,
    IHttpContextAccessor context,
    IUserService userService) : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionHandler> _logger = logger;
    private readonly IHttpContextAccessor _context = context;
    private readonly IUserService _userService = userService;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!context.User.Identity.IsAuthenticated)
        {
            _logger.LogInformation("User is unathenticated.");
            context.Fail();
            return;
        }

        var userDto = await _userService.GetWithPermissionsAsync(context.User.Email());
        if (userDto == null)
        {
            _logger.LogInformation("User does not exist.");
            context.Fail();
            return;
        }

        var result = requirement.ApplyOrCondition
            ? requirement.RequiredPermissions.Any(code => userDto.Permissions.Contains(code)) // At least one requirement permission is present
            : requirement.RequiredPermissions.All(code => userDto.Permissions.Contains(code)); // All required permissions are present
        if (!result)
        {
            _logger.LogInformation("User does not have the required permissions.");
            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }
}
