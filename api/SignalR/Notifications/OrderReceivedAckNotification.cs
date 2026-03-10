using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Models;
using Scv.Api.Models.Order;
using Scv.Api.Services;

namespace Scv.Api.SignalR;

public class OrderReceivedAckNotification
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderReceivedAckNotification> _logger;

    public OrderReceivedAckNotification(
        INotificationService notificationService,
        ILogger<OrderReceivedAckNotification> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task SendAsync(OrderDto order, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning(
                "Order received notification skipped. No user found for userId {userId}.",
                userId);
            return;
        }

        var payload = new OrderReceivedNotificationPayload(
            order.Id,
            order.OrderRequest.CourtFile.PhysicalFileId.ToString(),
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
