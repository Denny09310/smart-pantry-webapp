using System.Collections.Concurrent;

namespace Server.Services;

/// <summary>
/// Who is currently connected to the cosmetics hub, per household member.
/// Anonymous connections (no or unknown member id) still receive broadcasts
/// but never appear in the roster. Connection counts per member collapse to
/// a set: one member with three tabs open is simply online.
/// </summary>
public sealed class PresenceTracker
{
    private readonly ConcurrentDictionary<string, string> _connections = new();

    public IReadOnlyList<string> Connect(string connectionId, string? memberId)
    {
        if (memberId is not null)
            _connections[connectionId] = memberId;
        else
            _connections.TryRemove(connectionId, out _);

        return OnlineIds;
    }

    public IReadOnlyList<string> Disconnect(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
        return OnlineIds;
    }

    public IReadOnlyList<string> OnlineIds =>
        _connections.Values.Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList();
}
