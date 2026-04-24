namespace Scv.Api.SignalR;

public class PostgresSignalRBackplaneOptions(
    string channel,
    string instanceId,
    int maxPayloadBytes,
    int outboxPollSeconds,
    int outboxBatchSize,
    int outboxRetentionMinutes,
    int outboxMinAgeSeconds)
{

    // The listen/notify channel to use for this backplane.
    public string Channel { get; } = channel;
    // The unique instance id of this server.
    public string InstanceId { get; } = instanceId;
    // The max payload size before compression is used. NOTE: recommend keeping message size below this limit as a best-practice.
    public int MaxPayloadBytes { get; } = maxPayloadBytes;
    // The number of seconds to poll the outbox for unclaimed messages. Polling is used in case a listen/notify message is lost.
    public int OutboxPollSeconds { get; } = outboxPollSeconds;
    // The size of the batch to be handled by the polling logic. Minimizes server overhead in the event of a large fan-out or sustained loss of listen/notify messages.
    public int OutboxBatchSize { get; } = outboxBatchSize;
    // How long messages should be retained in the inbox before deletion. Outbox messages are intended to be short-lived.
    public int OutboxRetentionMinutes { get; } = outboxRetentionMinutes;
    // The amount of time to avoid new outbox messages when polling. Ensures that listen/notify messages are handled first, polling is only used for failures.
    public int OutboxMinAgeSeconds { get; } = outboxMinAgeSeconds;
}
