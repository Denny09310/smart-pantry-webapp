using System.Net.Http.Json;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;
using Server.Data.Entities;

using Shared.Models;

namespace Server.Tests;

public class PantryHubTests(PantryApiFactory factory) : IClassFixture<PantryApiFactory>
{
    [Fact]
    public async Task Create_Broadcasts_PantryChanged()
    {
        var name = $"test-hub-{Guid.CreateVersion7()}";
        PantryItem? created = null;
        var change = await ActAndCaptureAsync(async () =>
        {
            using var create = await factory.CreateClient().PostAsJsonAsync("/api/pantry",
                new CreatePantryItemRequest(
                    name, 1, "pcs", "pantry", null, DateOnly.FromDateTime(DateTime.Today.AddDays(3))));
            create.EnsureSuccessStatusCode();
            // Capture here: the helper's cleanup deletes the row before returning.
            created = await FindByNameAsync(name);
        }, name);

        Assert.Equal(PantryChangeKind.Created, change.ChangeKind);
        Assert.NotNull(created);
        Assert.Equal(created.Id, change.ItemId);
    }

    [Fact]
    public async Task Delete_Broadcasts_PantryRemoved()
    {
        var name = $"test-hub-{Guid.CreateVersion7()}";
        string id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = new PantryItem
            {
                Name = name,
                Quantity = 1,
                Unit = "pcs",
                Location = "pantry",
                ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
            };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            id = item.Id;
        }

        var change = await ActAndCaptureAsync(async () =>
        {
            using var del = await factory.CreateClient().DeleteAsync($"/api/pantry/{id}");
            del.EnsureSuccessStatusCode();
        }, name);

        Assert.Equal(PantryChangeKind.Removed, change.ChangeKind);
        Assert.Equal(id, change.ItemId);
    }

    private async Task<PantryChangedEvent> ActAndCaptureAsync(Func<Task> act, string name)
    {
        var tcs = new TaskCompletionSource<PantryChangedEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/pantry"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        connection.On<PantryChangedEvent>(
            nameof(IPantryHubClient.PantryChanged),
            change =>
            {
                tcs.TrySetResult(change);
                return Task.CompletedTask;
            });

        await connection.StartAsync();

        try
        {
            await act();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            return await tcs.Task.WaitAsync(cts.Token);
        }
        finally
        {
            await DeleteItemsByNameAsync(name);
            await connection.StopAsync();
        }
    }

    private async Task<PantryItem?> FindByNameAsync(string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Name == name);
    }

    private async Task DeleteItemsByNameAsync(string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Items.Where(x => x.Name == name).ExecuteDeleteAsync();
    }
}