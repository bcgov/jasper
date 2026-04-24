using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Order;

namespace Scv.Api.SignalR.Notifications;

public class OrderReceivedAckNotification(
    INotificationService notificationService,
    ILogger<OrderReceivedAckNotification> logger)
{
    private readonly INotificationService _notificationService = notificationService;
    private readonly ILogger<OrderReceivedAckNotification> _logger = logger;

    public async Task SendAsync(OrderDto order, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning(
                "Order received notification skipped. No user found for userId {UserId}.",
                userId);
            return;
        }

        var payload = new OrderReceivedNotificationPayload(
            order.Id,
            order.OrderRequest.PhysicalFileId.ToString(),
            "Order received.");

        var notification = new NotificationDto<OrderReceivedNotificationPayload>(
            Type: NotificationType.ORDER_RECEIVED,
            Timestamp: DateTimeOffset.UtcNow,
            Payload: payload,
            ReferenceId: order.Id,
            OfflineMinutes: 30
        );

        await _notificationService.NotifyUserWithAckAsync(
            userId,
            notification);
    }
}
