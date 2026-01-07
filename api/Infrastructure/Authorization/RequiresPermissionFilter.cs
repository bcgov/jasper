using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Scv.Core.Infrastructure.Authorization;
using System.Threading.Tasks;

namespace Scv.Api.Infrastructure.Authorization;

public class RequiresPermissionFilter(IAuthorizationService authService, PermissionRequirement requiredPermissions) : IAsyncAuthorizationFilter
{
    private readonly IAuthorizationService _authService = authService;
    private readonly PermissionRequirement _requiredPermissions = requiredPermissions;


    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var result = await _authService.AuthorizeAsync(
            context.HttpContext.User,
            context.ActionDescriptor.DisplayName,
            _requiredPermissions);

        if (!result.Succeeded)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Access Denied",
                Detail = "Insufficient permissions."
            });
        }

        await Task.CompletedTask;
    }
}
