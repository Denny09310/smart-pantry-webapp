using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

using Shared.Models;

namespace Client.Services;

/// <summary>
/// Realtime pantry broadcasts over the typed SignalR hub, plus member presence.
/// Broadcasts are hints, never required: receivers re-query the endpoints,
/// and a dead connection leaves manual reload fully working.
/// The connection identifies as the current member (?memberId=); switching
/// members reconnects so presence follows the user, not the device.
/// </summary>
internal sealed class PantryRealtimeService(
    NavigationManager nav,
    MemberService members,
    ILogger<PantryRealtimeService> log) : IAsyncDisposable
{
    public event Func<PantryChangedEvent, Task>? Changed;
    public event Action? PresenceChanged;

    private HubConnection? _connection;
    private string? _memberId;
    private bool _disposed;
    private int _connectGeneration;

    private readonly HashSet<string> _onlineIds = new(StringComparer.Ordinal);

    public IReadOnlySet<string> OnlineMemberIds => _onlineIds;

    public bool IsMemberOnline(string? memberId) =>
        memberId is not null && _onlineIds.Contains(memberId);

    private bool _subscribed;

    public async Task StartAsync()
    {
        if (_connection is not null || _disposed)
            return;

        // Subscribe once: a failed first connect leaves _connection null and
        // the next StartAsync would otherwise add a duplicate handler.
        if (!_subscribed)
        {
            members.Changed += OnMembersChanged;
            _subscribed = true;
        }

        _connection = await ConnectAsync();
    }

    private async void OnMembersChanged()
    {
        try
        {
            // Reconnect only when the identity actually changed: creates and
            // renames keep the same connection.
            if (members.CurrentMemberId != _memberId)
                await ReconnectAsync();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Realtime reconnect after member change failed.");
        }
    }

    private async Task ReconnectAsync()
    {
        if (_disposed)
            return;

        // Rapid member switches overlap: only the newest run may keep its
        // connection, so a stale run can neither leak one nor orphan roster state.
        var generation = ++_connectGeneration;
        var old = _connection;
        _connection = null;
        _onlineIds.Clear();
        PresenceChanged?.Invoke();

        if (old is not null)
            await old.DisposeAsync();

        if (_disposed || generation != _connectGeneration)
            return;

        var fresh = await ConnectAsync();
        if (_disposed || generation != _connectGeneration)
        {
            // Superseded while connecting: drop it so no ghost stays online.
            if (fresh is not null)
                await fresh.DisposeAsync();
            return;
        }

        _connection = fresh;
    }

    private async Task<HubConnection?> ConnectAsync()
    {
        _memberId = members.CurrentMemberId;
        var url = _memberId is null
            ? "/hubs/pantry"
            : $"/hubs/pantry?memberId={Uri.EscapeDataString(_memberId)}";

        var connection = new HubConnectionBuilder()
            .WithUrl(nav.ToAbsoluteUri(url))
            .WithAutomaticReconnect()
            .Build();

        connection.On<PantryChangedEvent>(
            nameof(IPantryHubClient.PantryChanged),
            async change =>
            {
                var handlers = Changed?.GetInvocationList();
                if (handlers is null)
                    return;

                foreach (Func<PantryChangedEvent, Task> handler in handlers)
                    await handler(change);
            });

        connection.On<IReadOnlyList<string>>(
            nameof(IPantryHubClient.PresenceChanged),
            roster =>
            {
                _onlineIds.Clear();
                foreach (var id in roster)
                    _onlineIds.Add(id);
                PresenceChanged?.Invoke();
                return Task.CompletedTask;
            });

        try
        {
            await connection.StartAsync();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Realtime hub unreachable; falling back to manual reload.");
            await connection.DisposeAsync();
            return null;
        }

        return connection;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        members.Changed -= OnMembersChanged;

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
