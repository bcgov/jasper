namespace Scv.Api.SignalR.Notifications;

public record OrderReceivedNotificationPayload(
    string OrderId,
    string PhysicalFileId,
    string Message);
