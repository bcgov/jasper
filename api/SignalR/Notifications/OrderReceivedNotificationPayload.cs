namespace Scv.Api.SignalR;

public record OrderReceivedNotificationPayload(
    string OrderId,
    string PhysicalFileId,
    string Message);
