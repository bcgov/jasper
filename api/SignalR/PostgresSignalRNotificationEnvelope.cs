namespace Scv.Api.SignalR;

public class PostgresSignalRNotificationEnvelope
{
    // Tracks the server that originated this notification
    public string SenderId { get; set; }
    // The intended scope of the notification. Ie: "user"
    public string Scope { get; set; }
    // The intended user for this message
    public string UserId { get; set; }
    // The intended method. Ie: "sendNotification"
    public string Method { get; set; }
    // Whether this notification payload is compressed.
    public bool Compressed { get; set; }
    // The notification's JSON payload.
    public string Payload { get; set; }
}
