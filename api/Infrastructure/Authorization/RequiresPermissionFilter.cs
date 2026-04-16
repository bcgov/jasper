using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
            context.Result = new ObjectResult(new
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Access Denied",
                Detail = "Insufficient permissions."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        await Task.CompletedTask;
    }
}
