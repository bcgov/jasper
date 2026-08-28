using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Order;

namespace Scv.Api.SignalR.Notifications;

public class OrderReceivedAckNotification(
    INotificationService notificationService,
    ILogger<OrderReceivedAckNotification> logger)
    : OrderAckNotificationBase<OrderReceivedNotificationPayload>(notificationService, logger)
{
    protected override NotificationType NotificationType => NotificationType.ORDER_RECEIVED;

    protected override OrderReceivedNotificationPayload BuildPayload(OrderDto order)
        => new(
            order.Id,
            order.OrderRequest.PhysicalFileId.ToString(),
            "Order received.");
}
