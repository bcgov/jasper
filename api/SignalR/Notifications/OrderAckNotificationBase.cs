using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Order;

namespace Scv.Api.SignalR.Notifications;

public abstract class OrderAckNotificationBase<TPayload>(
    INotificationService notificationService,
    ILogger logger)
{
    private readonly ILogger _logger = logger;
    private readonly INotificationService _notificationService = notificationService;

    private const int OfflineNotificationMinutes = 30;

    protected abstract NotificationType NotificationType { get; }

    protected abstract TPayload BuildPayload(OrderDto order);

    public async Task SendAsync(OrderDto order, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning(
                "Order notification skipped. No user found for userId {UserId}.",
                userId);
            return;
        }

        var notification = new NotificationDto<TPayload>(
            Type: NotificationType,
            Timestamp: DateTimeOffset.UtcNow,
            Payload: BuildPayload(order),
            ReferenceId: order.Id,
            OfflineMinutes: OfflineNotificationMinutes
        );

        await _notificationService.NotifyUserWithAckAsync(userId, notification);
    }
}
