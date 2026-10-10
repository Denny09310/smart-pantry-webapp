using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

using Server.Services;

using Shared.Models;

namespace Server.Tests;

[Collection("api")]
public sealed class NotificationEndpointsTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueName(string prefix)
        => $"{prefix}-{Guid.NewGuid():N}";

    private static DateOnly InDays(int days)
        => DateOnly.FromDateTime(DateTime.Today).AddDays(days);

    private async Task<string> CreateItemAsync(DateOnly expires)
    {
        var request = new CreatePantryItemRequest(UniqueName("notified"), 1, "pcs", "pantry", null, expires);

        using var response = await _client.PostAsJsonAsync("/api/pantry", request);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(created);
        return created.Id;
    }

    private async Task DeleteItemAsync(string id)
    {
        using var response = await _client.DeleteAsync($"/api/pantry/{id}");
        response.EnsureSuccessStatusCode();
    }

    private static async Task<NotificationService.GenerationResult> GenerateAsync(PantryApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<NotificationService>();
        return await service.GenerateExpirationNotificationsAsync();
    }

    private async Task<GetNotificationsResponse> ListAsync()
    {
        using var response = await _client.GetAsync("/api/notifications");
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<GetNotificationsResponse>();
        Assert.NotNull(page);
        return page;
    }

    [Fact]
    public async Task Generate_Is_Idempotent_Until_Read()
    {
        var itemId = await CreateItemAsync(InDays(1));
        try
        {
            Assert.True((await GenerateAsync(factory)).Created >= 1);

            var mine = (await ListAsync()).Items.Where(n => n.PantryItemId == itemId).ToList();
            Assert.NotEmpty(mine);
            Assert.Equal(InDays(1), mine[0].ExpirationDate);

            // Second run creates nothing while the first is still unread.
            Assert.Equal(0, (await GenerateAsync(factory)).Created);
        }
        finally
        {
            await DeleteItemAsync(itemId);
        }
    }

    [Fact]
    public async Task Read_And_ReadAll_Flow()
    {
        var itemId = await CreateItemAsync(InDays(2));
        try
        {
            await GenerateAsync(factory);

            var before = await ListAsync();
            var mine = before.Items.FirstOrDefault(n => n.PantryItemId == itemId && n.ReadAt == null);
            Assert.NotNull(mine);

            using var read = await _client.PostAsync($"/api/notifications/{mine.Id}/read", null);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            var updated = await read.Content.ReadFromJsonAsync<NotificationDto>();
            Assert.NotNull(updated);
            Assert.NotNull(updated.ReadAt);

            using var all = await _client.PostAsync("/api/notifications/read-all", null);
            Assert.Equal(HttpStatusCode.OK, all.StatusCode);

            Assert.Equal(0, (await ListAsync()).UnreadCount);
        }
        finally
        {
            await DeleteItemAsync(itemId);
        }
    }

    [Fact]
    public async Task MarkRead_Missing_Returns_NotFound()
    {
        using var response = await _client.PostAsync($"/api/notifications/{Guid.NewGuid():N}/read", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Generate_Respects_Seven_Day_Window()
    {
        var inside = new[]
        {
            await CreateItemAsync(InDays(-1)),
            await CreateItemAsync(InDays(0)),
            await CreateItemAsync(InDays(7)),
        };
        var outside = new[]
        {
            await CreateItemAsync(InDays(8)),
            await CreateItemAsync(InDays(30)),
        };

        try
        {
            Assert.Equal(3, (await GenerateAsync(factory)).Created);

            var notified = (await ListAsync()).Items
                .Where(n => n.ReadAt == null)
                .Select(n => n.PantryItemId)
                .ToHashSet();
            Assert.All(inside, id => Assert.Contains(id, notified));
            Assert.All(outside, id => Assert.DoesNotContain(id, notified));
        }
        finally
        {
            foreach (var id in inside.Concat(outside))
                await DeleteItemAsync(id);
        }
    }

    [Fact]
    public async Task Generate_With_No_Qualifying_Items_Returns_Zero()
    {
        var itemId = await CreateItemAsync(InDays(30));
        try
        {
            Assert.Equal(0, (await GenerateAsync(factory)).Created);
        }
        finally
        {
            await DeleteItemAsync(itemId);
        }
    }
}