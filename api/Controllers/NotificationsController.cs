using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Models;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationPublisher publisher) : ControllerBase
{
    private readonly INotificationPublisher _publisher = publisher;

    [HttpPost("demo")]
    public async Task<IActionResult> SendDemo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var serverId = Environment.MachineName;
        Response.Headers["X-Server-Id"] = serverId;

        var notification = new NotificationDto(
            Type: "demo",
            Message: $"Hello from the server! server id: ({serverId})",
            Timestamp: DateTimeOffset.UtcNow,
            Payload: new { source = "demo-endpoint" }
        );

        await _publisher.NotifyUserAsync(userId, notification);

        return Ok();
    }
}
