using System;
using System.Collections.Concurrent;

namespace Scv.Api.SignalR;

public class UserConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _connections =
        new(StringComparer.Ordinal);

    public void AddConnection(string userId, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        var userConnections = _connections.GetOrAdd(
            userId,
            _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
        userConnections.TryAdd(connectionId, 0);
    }

    public void RemoveConnection(string userId, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        if (!_connections.TryGetValue(userId, out var userConnections))
        {
            return;
        }

        userConnections.TryRemove(connectionId, out _);
        if (userConnections.IsEmpty)
        {
            _connections.TryRemove(userId, out _);
        }
    }

    public bool HasConnections(string userId)
    {
        return !string.IsNullOrWhiteSpace(userId) &&
            _connections.TryGetValue(userId, out var userConnections) &&
            !userConnections.IsEmpty;
    }
}
