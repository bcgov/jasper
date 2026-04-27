namespace Scv.Api.SignalR;

public class PostgresSignalRBackplaneOptions
{
    public PostgresSignalRBackplaneOptions(
        string channel,
        string instanceId,
        int maxPayloadBytes,
        int outboxPollSeconds,
        int outboxBatchSize,
        int outboxRetentionMinutes,
        int outboxMinAgeSeconds)
    {
        Channel = channel;
        InstanceId = instanceId;
        MaxPayloadBytes = maxPayloadBytes;
        OutboxPollSeconds = outboxPollSeconds;
        OutboxBatchSize = outboxBatchSize;
        OutboxRetentionMinutes = outboxRetentionMinutes;
        OutboxMinAgeSeconds = outboxMinAgeSeconds;
    }

    // The listen/notify channel to use for this backplane.
    public string Channel { get; }
    // The unique instance id of this server.
    public string InstanceId { get; }
    // The max payload size before compression is used. NOTE: recommend keeping message size below this limit as a best-practice.
    public int MaxPayloadBytes { get; }
    // The number of seconds to poll the outbox for unclaimed messages. Polling is used in case a listen/notify message is lost.
    public int OutboxPollSeconds { get; }
    // The size of the batch to be handled by the polling logic. Minimizes server overhead in the event of a large fan-out or sustained loss of listen/notify messages.
    public int OutboxBatchSize { get; }
    // How long messages should be retained in the inbox before deletion. Outbox messages are intended to be short-lived.
    public int OutboxRetentionMinutes { get; }
    // The amount of time to avoid new outbox messages when polling. Ensures that listen/notify messages are handled first, polling is only used for failures.
    public int OutboxMinAgeSeconds { get; }
}
