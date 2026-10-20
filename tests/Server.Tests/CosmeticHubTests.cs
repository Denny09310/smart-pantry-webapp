using System.Net.Http.Json;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;
using Server.Data.Entities;

using Shared.Models;

namespace Server.Tests;

public class CosmeticHubTests(CosmeticApiFactory factory) : IClassFixture<CosmeticApiFactory>
{
    [Fact]
    public async Task Create_Broadcasts_CosmeticChanged()
    {
        var name = $"test-hub-{Guid.CreateVersion7()}";
        CosmeticItem? created = null;
        var change = await ActAndCaptureAsync(async () =>
        {
            using var create = await factory.CreateClient().PostAsJsonAsync("/api/cosmetics",
                new CreateCosmeticItemRequest(
                    name, 1, "pcs", "body", null, DateOnly.FromDateTime(DateTime.Today.AddDays(3))));
            create.EnsureSuccessStatusCode();
            // Capture here: the helper's cleanup deletes the row before returning.
            created = await FindByNameAsync(name);
        }, name);

        Assert.Equal(CosmeticChangeKind.Created, change.ChangeKind);
        Assert.NotNull(created);
        Assert.Equal(created.Id, change.ItemId);
    }

    [Fact]
    public async Task Delete_Broadcasts_CosmeticRemoved()
    {
        var name = $"test-hub-{Guid.CreateVersion7()}";
        string id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = new CosmeticItem
            {
                Name = name,
                Quantity = 1,
                Unit = "pcs",
                Category = "body",
                ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
            };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            id = item.Id;
        }

        var change = await ActAndCaptureAsync(async () =>
        {
            using var del = await factory.CreateClient().DeleteAsync($"/api/cosmetics/{id}");
            del.EnsureSuccessStatusCode();
        }, name);

        Assert.Equal(CosmeticChangeKind.Removed, change.ChangeKind);
        Assert.Equal(id, change.ItemId);
    }

    [Fact]
    public async Task Update_Broadcasts_CosmeticUpdated()
    {
        var name = $"test-hub-{Guid.CreateVersion7()}";
        string id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = new CosmeticItem
            {
                Name = name,
                Quantity = 1,
                Unit = "pcs",
                Category = "body",
                ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
            };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            id = item.Id;
        }

        try
        {
            var change = await ActAndCaptureAsync(async () =>
            {
                using var put = await factory.CreateClient().PutAsJsonAsync($"/api/cosmetics/{id}",
                    new UpdateCosmeticItemRequest(
                        name, 2, "pcs", "face", null,
                        DateOnly.FromDateTime(DateTime.Today.AddDays(4))));
                put.EnsureSuccessStatusCode();
            }, name);

            Assert.Equal(CosmeticChangeKind.Updated, change.ChangeKind);
            Assert.Equal(id, change.ItemId);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, id);
        }
    }

    private async Task<CosmeticChangedEvent> ActAndCaptureAsync(Func<Task> act, string name)
    {
        var tcs = new TaskCompletionSource<CosmeticChangedEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/cosmetics"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        connection.On<CosmeticChangedEvent>(
            nameof(ICosmeticHubClient.CosmeticChanged),
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
            await CosmeticTestHelpers.DeleteItemsByNameAsync(factory.Services, name);
            await connection.StopAsync();
        }
    }

    [Fact]
    public async Task Connect_WithMember_BroadcastsPresence()
    {
        var memberId = await SeedMemberAsync();

        try
        {
            var roster = await ConnectAndCaptureRosterAsync($"?memberId={memberId}");
            Assert.Contains(memberId, roster);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteMemberAsync(factory.Services, memberId);
        }
    }

    [Fact]
    public async Task Connect_UnknownMember_StaysAnonymous()
    {
        var roster = await ConnectAndCaptureRosterAsync("?memberId=does-not-exist");

        Assert.DoesNotContain("does-not-exist", roster);
    }

    [Fact]
    public async Task Disconnect_RemovesFromRoster()
    {
        var memberId = await SeedMemberAsync();

        try
        {
            // The member's own connect echo proves registration completed.
            var memberTcs = new TaskCompletionSource<IReadOnlyList<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var memberConnection = await StartConnectionAsync(
                $"?memberId={memberId}",
                c => c.On<IReadOnlyList<string>>(
                    nameof(ICosmeticHubClient.PresenceChanged),
                    roster =>
                    {
                        memberTcs.TrySetResult(roster);
                        return Task.CompletedTask;
                    }));
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                Assert.Contains(memberId, await memberTcs.Task.WaitAsync(cts.Token));

            // The observer's own connect broadcast necessarily lists them now.
            var rosters = new List<IReadOnlyList<string>>();
            await using var observer = await StartConnectionAsync(
                null,
                c => c.On<IReadOnlyList<string>>(
                    nameof(ICosmeticHubClient.PresenceChanged),
                    roster =>
                    {
                        lock (rosters)
                            rosters.Add(roster);
                        return Task.CompletedTask;
                    }));
            Assert.Contains(memberId, await WaitForRosterAsync(rosters, 0));

            await memberConnection.StopAsync();
            Assert.DoesNotContain(memberId, await WaitForRosterAsync(rosters, 1));
        }
        finally
        {
            await CosmeticTestHelpers.DeleteMemberAsync(factory.Services, memberId);
        }
    }

    private async Task<IReadOnlyList<string>> ConnectAndCaptureRosterAsync(string? query)
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = await StartConnectionAsync(
            query,
            c => c.On<IReadOnlyList<string>>(
                nameof(ICosmeticHubClient.PresenceChanged),
                roster =>
                {
                    tcs.TrySetResult(roster);
                    return Task.CompletedTask;
                }));

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            return await tcs.Task.WaitAsync(cts.Token);
        }
        finally
        {
            await connection.StopAsync();
        }
    }

    private async Task<HubConnection> StartConnectionAsync(string? query, Action<HubConnection>? setup = null)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/cosmetics" + query), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        setup?.Invoke(connection);
        await connection.StartAsync();
        return connection;
    }

    private static async Task<IReadOnlyList<string>> WaitForRosterAsync(List<IReadOnlyList<string>> rosters, int index)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            lock (rosters)
            {
                if (rosters.Count > index)
                    return rosters[index];
            }

            await Task.Delay(50, cts.Token);
        }
    }

    private async Task<string> SeedMemberAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var member = new HouseholdMember
        {
            DisplayName = $"test-member-{Guid.CreateVersion7()}",
            Color = "#3b82f6"
        };
        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member.Id;
    }



    private async Task<CosmeticItem?> FindByNameAsync(string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Name == name);
    }
}
