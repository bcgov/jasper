using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Scv.Core.Helpers.Extensions;

namespace Scv.Core.Infrastructure.Authorization;

public class PermissionHandler(
    ILogger<PermissionHandler> logger,
    IHttpContextAccessor httpContextAccessor
    ) : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionHandler> _logger = logger;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogInformation("User is unathenticated.");
            context.Fail();
            return Task.CompletedTask;
        }

        if (context.User.IsActive() != true)
        {
            _logger.LogInformation("User is inactive.");
            context.Fail();
            return Task.CompletedTask;
        }

        var isAuthorized = context.User.HasPermissions(requirement.RequiredPermissions, requirement.ApplyOrCondition);
        if (!isAuthorized)
        {
            _logger.LogInformation(
                "User does not have the required permissions for {url}.",
                _httpContextAccessor.HttpContext?.Request.Path);
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
