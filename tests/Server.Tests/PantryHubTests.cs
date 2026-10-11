using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

using Shared.Models;

namespace Server.Tests;

/// <summary>
/// The typed hub broadcasts on every pantry write path and stays silent
/// on validation failure (validation short-circuits before the handler).
/// SignalR runs over the in-process test server via long polling.
/// </summary>
[Collection("api")]
public sealed class PantryHubTests(PantryApiFactory factory) : IAsyncDisposable
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly List<HubConnection> _connections = [];

    private static string UniqueName(string prefix)
        => $"{prefix}-{Guid.NewGuid():N}";

    private static DateOnly InDays(int days)
        => DateOnly.FromDateTime(DateTime.Today).AddDays(days);

    private async Task<List<PantryChangedEvent>> ConnectAsync()
    {
        var received = new List<PantryChangedEvent>();

        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/pantry"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        connection.On<PantryChangedEvent>(
            nameof(IPantryHubClient.PantryChanged),
            evt =>
            {
                lock (received)
                    received.Add(evt);

                return Task.CompletedTask;
            });

        await connection.StartAsync();
        _connections.Add(connection);

        return received;
    }

    private static async Task<PantryChangedEvent?> WaitForAsync(
        List<PantryChangedEvent> received,
        Func<PantryChangedEvent, bool> match)
    {
        for (var i = 0; i < 50; i++)
        {
            lock (received)
            {
                var hit = received.FirstOrDefault(match);

                if (hit is not null)
                    return hit;
            }

            await Task.Delay(100);
        }

        return null;
    }

    [Fact]
    public async Task Create_Broadcasts_Created_With_Item_Id()
    {
        var received = await ConnectAsync();
        var name = UniqueName("hub-create");

        using var response = await _client.PostAsJsonAsync(
            "/api/pantry",
            new CreatePantryItemRequest(name, 1, "pcs", "pantry", null, InDays(2)));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(created);

        try
        {
            var evt = await WaitForAsync(received, e => e.ItemId == created.Id);
            Assert.NotNull(evt);
            Assert.Equal(PantryChangedEvent.Created, evt.Kind);
        }
        finally
        {
            using var deleted = await _client.DeleteAsync($"/api/pantry/{created.Id}");
            deleted.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Update_Broadcasts_Updated_With_Item_Id()
    {
        var received = await ConnectAsync();
        var name = UniqueName("hub-update");

        using var created = await _client.PostAsJsonAsync(
            "/api/pantry",
            new CreatePantryItemRequest(name, 1, "pcs", "pantry", null, InDays(2)));
        created.EnsureSuccessStatusCode();
        var item = await created.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(item);

        // Drain the create broadcast so only the update can match below.
        await WaitForAsync(received, e => e.ItemId == item.Id && e.Kind == PantryChangedEvent.Created);

        try
        {
            using var updated = await _client.PutAsJsonAsync(
                $"/api/pantry/{item.Id}",
                new UpdatePantryItemRequest(name, 2, "pcs", "fridge", null, InDays(3)));
            updated.EnsureSuccessStatusCode();

            var evt = await WaitForAsync(
                received,
                e => e.ItemId == item.Id && e.Kind == PantryChangedEvent.Updated);
            Assert.NotNull(evt);
        }
        finally
        {
            using var deleted = await _client.DeleteAsync($"/api/pantry/{item.Id}");
            deleted.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Delete_Broadcasts_Removed_With_Item_Id()
    {
        var received = await ConnectAsync();
        var name = UniqueName("hub-delete");

        using var created = await _client.PostAsJsonAsync(
            "/api/pantry",
            new CreatePantryItemRequest(name, 1, "pcs", "pantry", null, InDays(2)));
        created.EnsureSuccessStatusCode();
        var item = await created.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(item);

        // Drain the create broadcast so only the removal can match below.
        await WaitForAsync(received, e => e.ItemId == item.Id && e.Kind == PantryChangedEvent.Created);

        using var deleted = await _client.DeleteAsync($"/api/pantry/{item.Id}");
        deleted.EnsureSuccessStatusCode();

        var evt = await WaitForAsync(
            received,
            e => e.ItemId == item.Id && e.Kind == PantryChangedEvent.Removed);
        Assert.NotNull(evt);
    }

    [Fact]
    public async Task Invalid_Create_Broadcasts_Nothing()
    {
        var received = await ConnectAsync();

        using var response = await _client.PostAsJsonAsync(
            "/api/pantry",
            new CreatePantryItemRequest(" ", 1, "pcs", "pantry", null, InDays(2)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await Task.Delay(1000);

        lock (received)
            Assert.Empty(received);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections)
            await connection.DisposeAsync();

        _connections.Clear();
    }
}
