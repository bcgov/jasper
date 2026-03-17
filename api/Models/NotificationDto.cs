using System;
using System.Text.Json.Serialization;

namespace Scv.Api.Models;

public enum NotificationType
{
    SYSTEM,
    ORDER_RECEIVED
}


public record NotificationDto<TPayload>(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] NotificationType Type, // The type of the notification, used by the frontend to determine how to parse/handle this message.
    DateTimeOffset Timestamp, // The timestamp of the message.
    TPayload Payload = default, // The message body.
    string ReferenceId = null, // A general-purpose identifier, that can be used with the type to determine a reference entity for this message.
    Guid? AckGuid = null, // Stable identifier used for client acknowledgements.
    bool AckRequired = false, // When this flag is set, the backend requests that the frontend application will ack received messages.
    int OfflineMinutes = 1 // The amount of time after creation where the poller will still process messages. The system will attempt to redeliver messages in case of failure/user offline until this time.
);
