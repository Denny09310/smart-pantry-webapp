using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

using Shared.Models;

namespace Client.Services;

/// <summary>
/// Realtime pantry broadcasts over the typed SignalR hub.
/// Broadcasts are hints, never required: receivers re-query the endpoints,
/// and a dead connection leaves manual reload fully working.
/// </summary>
public sealed class PantryRealtimeService(NavigationManager nav, ILogger<PantryRealtimeService> log) : IAsyncDisposable
{
    public event Func<PantryChangedEvent, Task>? Changed;

    private HubConnection? _connection;
    private bool _disposed;

    public async Task StartAsync()
    {
        if (_connection is not null || _disposed)
            return;

        var connection = new HubConnectionBuilder()
            .WithUrl(nav.ToAbsoluteUri("/hubs/pantry"))
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

        try
        {
            await connection.StartAsync();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Realtime hub unreachable; falling back to manual reload.");
            await connection.DisposeAsync();
            return;
        }

        _connection = connection;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}