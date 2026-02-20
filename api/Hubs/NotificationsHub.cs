using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Scv.Api.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    public override Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var config = httpContext?.RequestServices.GetService<IConfiguration>();
        var logger = httpContext?.RequestServices.GetService<ILogger<NotificationsHub>>();
        var allowedOrigin = config?.GetValue<string>("CORS_DOMAIN");
        var disableOriginCheck = config?.GetValue<bool>("DISABLE_SIGNALR_ORIGIN_CHECK") ?? false;
        var origin = httpContext?.Request.Headers["Origin"].ToString();

        logger?.LogInformation(
            "SignalR connect attempt. Origin={Origin}, CORS_DOMAIN={CorsDomain}",
            origin,
            allowedOrigin);

        if (disableOriginCheck)
        {
            logger?.LogWarning("SignalR origin check disabled via DISABLE_SIGNALR_ORIGIN_CHECK.");
        }
        else if (!string.IsNullOrWhiteSpace(allowedOrigin))
        {
            var allowedOrigins = allowedOrigin
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.Trim().Trim('"', '\''))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            logger?.LogInformation(
                "SignalR allowed origins resolved to {AllowedOrigins}",
                string.Join(";", allowedOrigins));

            if (allowedOrigins.Length > 0 &&
                (string.IsNullOrWhiteSpace(origin) ||
                 !allowedOrigins.Any(value => string.Equals(origin, value, StringComparison.OrdinalIgnoreCase))))
            {
                logger?.LogWarning(
                    "SignalR connection aborted due to origin mismatch. Origin={Origin}",
                    origin);
                Context.Abort();
                return Task.CompletedTask;
            }
        }

        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            if (logger != null)
            {
                var claims = Context.User?.Claims
                    .Select(claim => $"{claim.Type}={claim.Value}")
                    .ToArray() ?? Array.Empty<string>();
                logger.LogDebug(
                    "SignalR user claims: {Claims}",
                    string.Join(";", claims));
            }
            logger?.LogWarning("SignalR connection aborted due to missing user id claim.");
            Context.Abort();
            return Task.CompletedTask;
        }

        logger?.LogInformation("SignalR connection accepted for user {UserId}.", userId);

        return base.OnConnectedAsync();
    }
}
