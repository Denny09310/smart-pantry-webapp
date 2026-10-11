using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

using Shared.Models;

namespace Client.Services;

/// <summary>
/// Live pantry refresh over the typed <c>/hubs/pantry</c> hub: subscribes
/// to <see cref="IPantryHubClient.PantryChanged"/> (via <c>nameof</c>, so a
/// contract rename breaks the build instead of silently dropping events)
/// and notifies pages, which re-query. Progressive enhancement only —
/// every page keeps its manual reload as the fallback, and a failed
/// connection never surfaces to the user.
/// </summary>
internal sealed class PantryRealtimeService(NavigationManager nav, ILogger<PantryRealtimeService> log) : IAsyncDisposable
{
    private HubConnection? _connection;
    private bool _started;

    public event Func<Task>? Changed;

    public async Task StartAsync()
    {
        if (_started)
            return;

        _started = true;

        try
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(nav.ToAbsoluteUri("/hubs/pantry"))
                .WithAutomaticReconnect()
                .Build();

            _connection.On<PantryChangedEvent>(
                nameof(IPantryHubClient.PantryChanged),
                _ => NotifyAsync());

            _connection.Closed += OnClosed;
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            // Broadcasts are optional: manual reload covers everything.
            log.LogWarning(ex, "Realtime connection failed; falling back to manual reload.");
        }
    }

    private Task NotifyAsync()
    {
        var handlers = Changed;
        return handlers is null ? Task.CompletedTask : handlers();
    }

    private Task OnClosed(Exception? ex)
    {
        // Automatic reconnect handles drops; only unexpected closes are logged.
        if (ex is not null)
            log.LogWarning(ex, "Realtime connection closed.");

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
