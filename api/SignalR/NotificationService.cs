using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PostgreSQL.ListenNotify;
using Scv.Api.Models;
using Scv.Api.Services;
using Scv.Db.Models;

namespace Scv.Api.SignalR;

public class NotificationService(
    IPostgresNotificationService postgresNotifications,
    PostgresSignalRBackplaneOptions backplaneOptions,
    ScvDbContext dbContext,
    ILogger<NotificationService> logger) : INotificationService
{
    private readonly IPostgresNotificationService _postgresNotifications = postgresNotifications;
    private readonly PostgresSignalRBackplaneOptions _backplaneOptions = backplaneOptions;
    private readonly ScvDbContext _dbContext = dbContext;
    private readonly ILogger<NotificationService> _logger = logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Notify a user with the given notification, and do not prompt for an ack from the client (fire-and-forget).
    /// </summary>
    /// <param name="userId">The desired recipient's mongo userId.</param>
    /// <param name="notification">The notification to be sent.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task NotifyUserAsync<TPayload>(string userId, NotificationDto<TPayload> notification)
    {
        await PublishAsync("user", userId, notification, ackRequired: false);
    }

    /// <summary>
    /// Notify a user with the given notification, and prompt the client for an ack.
    /// </summary>
    /// <param name="userId">The desired recipient's mongo userId.</param>
    /// <param name="notification">The notification to be sent.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task NotifyUserWithAckAsync<TPayload>(
        string userId,
        NotificationDto<TPayload> notification)
    {
        await PublishAsync("user", userId, notification, ackRequired: true);
    }

    private async Task PublishAsync<TPayload>(
        string scope,
        string userId,
        NotificationDto<TPayload> notification,
        bool ackRequired)
    {
        if (!string.Equals(scope, "user", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("SignalR publish rejected because scope is unknown. Scope={Scope}", scope);
            return;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("SignalR publish rejected because user id is missing.");
            return;
        }

        var ackGuid = Guid.NewGuid();
        var notificationWithAck = notification with
        {
            AckGuid = ackGuid,
            AckRequired = ackRequired
        };

        if (!TryBuildEnvelopeJson(scope, userId, notificationWithAck, out var envelopeJson))
        {
            return;
        }

        var outboxMessage = new SignalROutboxMessage
        {
            Channel = _backplaneOptions.Channel,
            UserId = userId,
            EnvelopeJson = envelopeJson,
            AckRequired = ackRequired,
            AckGuid = ackGuid,
            OfflineMinutes = notification.OfflineMinutes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Always add an outbox row before publishing - this ensures that all messages are tracked in the outbox regardless if they are received via listen/notify or not.
        _dbContext.SignalROutboxMessages.Add(outboxMessage);
        await _dbContext.SaveChangesAsync();

        try
        {
            await _postgresNotifications.NotifyAsync(
                _backplaneOptions.Channel,
                ackGuid.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR backplane failed to send NOTIFY for message id {MessageId}", outboxMessage.Id);
        }

    }

    private bool TryBuildEnvelopeJson<TPayload>(
        string scope,
        string userId,
        NotificationDto<TPayload> notification,
        out string envelopeJson)
    {
        var notificationJson = JsonSerializer.Serialize(notification, SerializerOptions);
        var payload = notificationJson;

        var envelope = new PostgresSignalRNotificationEnvelope
        {
            SenderId = _backplaneOptions.InstanceId,
            Scope = scope,
            UserId = userId,
            Method = "sendNotification",
            Compressed = false,
            Payload = payload
        };

        envelopeJson = JsonSerializer.Serialize(envelope, SerializerOptions);
        if (PostgresSignalRPayloadCodec.GetUtf8Size(envelopeJson) > _backplaneOptions.MaxPayloadBytes)
        {
            payload = PostgresSignalRPayloadCodec.CompressToBase64(notificationJson);
            envelope.Compressed = true;
            envelope.Payload = payload;
            envelopeJson = JsonSerializer.Serialize(envelope, SerializerOptions);
        }

        if (PostgresSignalRPayloadCodec.GetUtf8Size(envelopeJson) > _backplaneOptions.MaxPayloadBytes)
        {
            _logger.LogWarning(
                "SignalR payload exceeds configured max payload size. Scope={Scope} UserId={UserId}",
                scope,
                userId ?? "");
            envelopeJson = string.Empty;
            return false;
        }

        return true;
    }

}
