namespace Scv.Api.SignalR.Notifications;

public record OrderSubmittedAckNotificationPayload(
    string OrderId,
    string Message);
