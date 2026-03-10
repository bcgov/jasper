using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PostgreSQL.ListenNotify;
using Scv.Api.Hubs;
using Scv.Api.Models;
using Scv.Db.Repositories;

namespace Scv.Api.SignalR;

public class PostgresSignalRNotificationBridge(
    IPostgresNotificationService postgresNotifications,
    IHubContext<NotificationsHub> hubContext,
    PostgresSignalRBackplaneOptions backplaneOptions,
    UserConnectionTracker connectionTracker,
    IServiceScopeFactory scopeFactory,
    ILogger<PostgresSignalRNotificationBridge> logger) : BackgroundService
{
    private readonly IPostgresNotificationService _postgresNotifications = postgresNotifications;
    private readonly IHubContext<NotificationsHub> _hubContext = hubContext;
    private readonly PostgresSignalRBackplaneOptions _backplaneOptions = backplaneOptions;
    private readonly UserConnectionTracker _connectionTracker = connectionTracker;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<PostgresSignalRNotificationBridge> _logger = logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _postgresNotifications.NotificationReceived += OnNotificationReceived;

        try
        {
            // poll for missed listen/notify messages.
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_backplaneOptions.OutboxPollSeconds));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await PollOutboxAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SignalR backplane outbox poll failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping.
        }
        finally
        {
            _postgresNotifications.NotificationReceived -= OnNotificationReceived;
        }
    }

    private async void OnNotificationReceived(object sender, PostgresNotificationEventArgs args)
    {
        try
        {
            if (!string.Equals(args.Channel, _backplaneOptions.Channel, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(args.Payload))
            {
                return;
            }

            if (!Guid.TryParse(args.Payload, out var ackGuid))
            {
                _logger.LogWarning("SignalR backplane received invalid ack guid payload.");
                return;
            }

            await ProcessMessageAckGuidAsync(ackGuid, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR backplane failed to process NOTIFY payload.");
        }
    }

    private async Task PollOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        // Ignore very new outbox rows to avoid race condition with listen/notify. Listen/notify first, and then use outbox if listen/notify missed.
        var minimumAge = DateTimeOffset.UtcNow.AddSeconds(-_backplaneOptions.OutboxMinAgeSeconds);
        var pending = await repository.GetPendingAsync(
            _backplaneOptions.Channel,
            minimumAge,
            _backplaneOptions.OutboxBatchSize,
            cancellationToken);

        foreach (var message in pending)
        {
            await ProcessMessageAsync(repository, message, claimBeforeDeliver: true, cancellationToken);
        }
    }

    private async Task ProcessMessageAckGuidAsync(Guid ackGuid, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        var message = await repository.GetByAckGuidNoTrackingAsync(ackGuid, cancellationToken);

        if (message == null)
        {
            return;
        }

        await ProcessMessageAsync(repository, message, claimBeforeDeliver: true, cancellationToken);
    }

    private async Task ProcessMessageAsync(
        INotificationRepository repository,
        Scv.Db.Models.SignalROutboxMessage message,
        bool claimBeforeDeliver,
        CancellationToken cancellationToken)
    {
        if (!TryBuildNotification(message, out var envelope, out var notification))
        {
            return;
        }

        if (string.Equals(envelope.Scope, "user", StringComparison.OrdinalIgnoreCase))
        {
            var targetUserId = string.IsNullOrWhiteSpace(message.UserId)
                ? envelope.UserId
                : message.UserId;

            if (string.IsNullOrWhiteSpace(targetUserId))
            {
                _logger.LogWarning(
                    "SignalR backplane user-scoped message missing user id. OutboxId={OutboxId} AckGuid={AckGuid}",
                    message.Id,
                    message.AckGuid);
                return;
            }

            if (!_connectionTracker.HasConnections(targetUserId))
            {
                return;
            }

            if (claimBeforeDeliver)
            {
                var claimed = await repository.TryClaimDeliveryAsync(
                    message.Id,
                    _backplaneOptions.InstanceId,
                    cancellationToken);

                if (!claimed)
                {
                    return;
                }
            }

            var delivered = await DeliverNotificationAsync(
                envelope,
                notification,
                targetUserId,
                cancellationToken);
            if (!delivered)
            {
                // unclaim, the poll will re-attempt delivery.
                await repository.ResetDeliveryAsync(message.Id, cancellationToken);
            }

            return;
        }

        _logger.LogWarning(
            "SignalR backplane ignoring unsupported message scope. Scope={Scope} OutboxId={OutboxId} AckGuid={AckGuid}",
            envelope.Scope,
            message.Id,
            message.AckGuid);
    }

    private bool TryBuildNotification(
        Scv.Db.Models.SignalROutboxMessage message,
        out PostgresSignalRNotificationEnvelope envelope,
        out NotificationDto<JsonElement?> notification)
    {
        envelope = null;
        notification = null;
        if (string.IsNullOrWhiteSpace(message.EnvelopeJson))
        {
            return false;
        }

        try
        {
            envelope = JsonSerializer.Deserialize<PostgresSignalRNotificationEnvelope>(
                message.EnvelopeJson,
                SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "SignalR backplane received invalid payload.");
            return false;
        }

        if (envelope == null || string.IsNullOrWhiteSpace(envelope.Method))
        {
            return false;
        }

        var notificationJson = envelope.Payload ?? string.Empty;
        if (envelope.Compressed)
        {
            try
            {
                notificationJson = PostgresSignalRPayloadCodec.DecompressFromBase64(notificationJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR backplane failed to decompress payload.");
                return false;
            }
        }

        try
        {
            notification = JsonSerializer.Deserialize<NotificationDto<JsonElement?>>(
                notificationJson,
                SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "SignalR backplane failed to deserialize notification payload.");
            return false;
        }

        if (notification == null)
        {
            return false;
        }

        notification = notification with
        {
            AckGuid = message.AckGuid ?? notification.AckGuid,
            AckRequired = message.AckRequired
        };

        return true;
    }

    private async Task<bool> DeliverNotificationAsync(
        PostgresSignalRNotificationEnvelope envelope,
        NotificationDto<JsonElement?> notification,
        string targetUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients.User(targetUserId)
                .SendAsync(envelope.Method, notification, cancellationToken);
            _logger.LogInformation(
                "SignalR notification sent. Scope=User Recipient={UserId} Type={Type}",
                targetUserId,
                notification.Type);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR backplane failed to deliver notification.");
            return false;
        }
    }

}
