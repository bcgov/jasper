using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Extensions;
using Scv.Api.SignalR;
using Scv.Db.Repositories;

namespace Scv.Api.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    private readonly UserConnectionTracker _connectionTracker;

    public NotificationsHub(UserConnectionTracker connectionTracker)
    {
        _connectionTracker = connectionTracker;
    }

    /// <summary>
    /// Validates origin and user of this connection in order to safely send/receive notifications.
    /// </summary>
    /// <remarks>
    /// Uses server-side auth to validate that this user is authorized to ack this message.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var config = httpContext?.RequestServices.GetService<IConfiguration>();
        var logger = httpContext?.RequestServices.GetService<ILogger<NotificationsHub>>();
        var allowedOrigin = config?.GetValue<string>("PublicCorsDomain");
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
                throw new HubException("Connection rejected: origin not allowed.");
            }
        }

        var userId = Context.User?.UserId();
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
            throw new HubException("Connection rejected: authenticated user could not be identified.");
        }

        logger?.LogInformation("SignalR connection accepted for user {UserId}.", userId);

        // Track users connected to this server, so that this server only attempts to send notifications to users with an active connection.
        _connectionTracker.AddConnection(userId, Context.ConnectionId);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.User?.UserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            _connectionTracker.RemoveConnection(userId, Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Acknowledges a notification for the current user by marking it as read. Note that an ack just indicates the client JASPER app received the notification - the app is responsible for presenting 
    /// notifications to users.
    /// </summary>
    /// <param name="ackGuid">The stable identifier of the notification to acknowledge.</param>
    /// <remarks>
    /// Uses server-side auth to validate that this user is authorized to ack this message.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task AckNotification(Guid ackGuid)
    {
        var logger = Context.GetHttpContext()
            ?.RequestServices.GetService<ILogger<NotificationsHub>>();
        var userId = Context.User?.UserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger?.LogWarning(
                "SignalR ack failed due to missing user id. AckGuid={AckGuid}",
                ackGuid);
            return;
        }

        var repository = Context.GetHttpContext()
            ?.RequestServices.GetService<INotificationRepository>();
        if (repository == null)
        {
            logger?.LogWarning(
                "SignalR ack failed due to missing notification repository. AckGuid={AckGuid} UserId={UserId}",
                ackGuid,
                userId);
            return;
        }

        var matches = await repository.FindAsync(message => message.AckGuid == ackGuid);
        var message = matches.FirstOrDefault();
        if (message == null)
        {
            logger?.LogWarning(
                "SignalR ack failed to find message. AckGuid={AckGuid} UserId={UserId}",
                ackGuid,
                userId);
            return;
        }

        if (!IsAckAuthorized(message.UserId, userId, out var reason))
        {
            logger?.LogWarning(
                "SignalR ack rejected. AckGuid={AckGuid} UserId={UserId} Reason={Reason}",
                ackGuid,
                userId,
                reason);
            return;
        }

        if (!message.AckRequired)
        {
            logger?.LogInformation(
                "SignalR ack skipped because ack is not required. AckGuid={AckGuid} UserId={UserId}",
                ackGuid,
                userId);
            return;
        }

        if (message.AckedAt.HasValue)
        {
            logger?.LogInformation(
                "SignalR ack skipped because message already acked. AckGuid={AckGuid} UserId={UserId}",
                ackGuid,
                userId);
            return;
        }

        message.AckedAt = DateTimeOffset.UtcNow;
        message.AckedBy = userId;
        await repository.UpdateAsync(message);

        logger?.LogInformation(
            "SignalR ack accepted. AckGuid={AckGuid} UserId={UserId}",
            ackGuid,
            userId);
    }

    private static bool IsAckAuthorized(string messageUserId, string userId, out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrWhiteSpace(messageUserId))
        {
            reason = "UserMissing";
            return false;
        }

        if (!string.Equals(messageUserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            reason = "UserMismatch";
            return false;
        }

        return true;
    }
}
