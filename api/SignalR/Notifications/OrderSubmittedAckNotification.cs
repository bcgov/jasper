using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Order;

namespace Scv.Api.SignalR.Notifications;

public class OrderSubmittedAckNotification(
    INotificationService notificationService,
    ILogger<OrderSubmittedAckNotification> logger)
    : OrderAckNotificationBase<OrderSubmittedAckNotificationPayload>(notificationService, logger)
{
    protected override NotificationType NotificationType => NotificationType.ORDER_SUBMITTED;

    protected override OrderSubmittedAckNotificationPayload BuildPayload(OrderDto order)
        => new(
            order.Id,
            "Order submitted.");
}
