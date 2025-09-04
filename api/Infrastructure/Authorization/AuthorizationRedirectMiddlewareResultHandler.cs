using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Scv.Api.Infrastructure.Authorization
{
    public class AuthorizationRedirectMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();
        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden && authorizeResult.AuthorizationFailure?.FailureReasons.Any(fr => fr.Handler is ProviderAuthorizationHandler) == true)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict; // TODO: determine if there is a more graceful way to allow logout via XHR. Alternatively, implement a logout screen.
                return;
            }
            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
