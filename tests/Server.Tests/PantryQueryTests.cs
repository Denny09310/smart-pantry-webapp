using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;
using Server.Data.Entities;

using Shared.Models;

namespace Server.Tests;

public class PantryQueryTests(PantryApiFactory factory) : IClassFixture<PantryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Paging_ReturnsStableOrderedSlices()
    {
        var prefix = $"test-page-{Guid.CreateVersion7()}";
        var today = DateOnly.FromDateTime(DateTime.Today);
        var ids = new List<string>();
        for (var i = 0; i < 5; i++)
            ids.Add((await PantryTestHelpers.SeedItemAsync(factory.Services, $"{prefix}-{i:00}", today.AddDays(30 - i))).Id);

        try
        {
            var first = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={prefix}&skip=0&take=2");
            var second = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={prefix}&skip=2&take=2");
            var third = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={prefix}&skip=4&take=2");

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotNull(third);
            Assert.Equal(5, first.TotalItems);

            var all = first.Items.Concat(second.Items).Concat(third.Items).ToList();
            Assert.Equal(5, all.Count);
            Assert.Equal(5, all.Select(x => x.Id).Distinct().Count());
            Assert.Equal(all.OrderBy(x => x.ExpirationDate).Select(x => x.Id), all.Select(x => x.Id));
        }
        finally
        {
            foreach (var id in ids)
                await PantryTestHelpers.DeleteItemAsync(factory.Services, id);
        }
    }

    [Fact]
    public async Task StatusFilter_ReturnsOnlyMatchingBucket()
    {
        var prefix = $"test-status-{Guid.CreateVersion7()}";
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expired = await PantryTestHelpers.SeedItemAsync(factory.Services, $"{prefix}-expired", today.AddDays(-1));
        var soon = await PantryTestHelpers.SeedItemAsync(factory.Services, $"{prefix}-soon", today.AddDays(2));
        var fresh = await PantryTestHelpers.SeedItemAsync(factory.Services, $"{prefix}-fresh", today.AddDays(30));

        try
        {
            var expiredOnly = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={prefix}&status=expired");
            var soonOnly = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={prefix}&status=soon");
            var freshOnly = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={prefix}&status=fresh");

            Assert.NotNull(expiredOnly);
            Assert.NotNull(soonOnly);
            Assert.NotNull(freshOnly);
            Assert.Single(expiredOnly.Items, x => x.Id == expired.Id);
            Assert.Single(soonOnly.Items, x => x.Id == soon.Id);
            Assert.Single(freshOnly.Items, x => x.Id == fresh.Id);
        }
        finally
        {
            await PantryTestHelpers.DeleteItemAsync(factory.Services, expired.Id);
            await PantryTestHelpers.DeleteItemAsync(factory.Services, soon.Id);
            await PantryTestHelpers.DeleteItemAsync(factory.Services, fresh.Id);
        }
    }

    [Fact]
    public async Task StatusAll_ReturnsUnfiltered()
    {
        var prefix = $"test-all-{Guid.CreateVersion7()}";
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expired = await PantryTestHelpers.SeedItemAsync(factory.Services, $"{prefix}-expired", today.AddDays(-1));
        var fresh = await PantryTestHelpers.SeedItemAsync(factory.Services, $"{prefix}-fresh", today.AddDays(30));

        try
        {
            using var response = await _client.GetAsync($"/api/pantry?name={prefix}&status=all");
            response.EnsureSuccessStatusCode();
            var page = await response.Content.ReadFromJsonAsync<GetPantryItemsResponse>();
            Assert.NotNull(page);
            var ids = page.Items.Select(x => x.Id).ToList();
            Assert.Contains(expired.Id, ids);
            Assert.Contains(fresh.Id, ids);
        }
        finally
        {
            await PantryTestHelpers.DeleteItemAsync(factory.Services, expired.Id);
            await PantryTestHelpers.DeleteItemAsync(factory.Services, fresh.Id);
        }
    }

    [Fact]
    public async Task LocationFilter_IsCaseInsensitiveExactMatch()
    {
        var prefix = $"test-loc-{Guid.CreateVersion7()}";
        var today = DateOnly.FromDateTime(DateTime.Today);
        string id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = new PantryItem
            {
                Name = $"{prefix}-fridge",
                Quantity = 1,
                Unit = "pcs",
                Location = "Fridge",
                ExpirationDate = today.AddDays(5)
            };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            id = item.Id;
        }

        try
        {
            var upper = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={prefix}&location=FRIDGE");
            Assert.NotNull(upper);
            Assert.Contains(upper.Items, x => x.Id == id);

            var partial = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={prefix}&location=frid");
            Assert.NotNull(partial);
            Assert.DoesNotContain(partial.Items, x => x.Id == id);
        }
        finally
        {
            await PantryTestHelpers.DeleteItemAsync(factory.Services, id);
        }
    }

    [Fact]
    public async Task SortName_ReturnsAlphabeticalOrder()
    {
        var prefix = $"test-sort-{Guid.CreateVersion7()}";
        var first = await PantryTestHelpers.SeedItemAsync(factory.Services, $"{prefix}-cherry", DateOnly.FromDateTime(DateTime.Today.AddDays(5)));
        var second = await PantryTestHelpers.SeedItemAsync(factory.Services, $"{prefix}-apple", DateOnly.FromDateTime(DateTime.Today.AddDays(5)));

        try
        {
            var body = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={prefix}&sort=name");

            Assert.NotNull(body);
            Assert.Equal(
                new[] { $"{prefix}-apple", $"{prefix}-cherry" },
                body.Items.Select(x => x.Name));
        }
        finally
        {
            await PantryTestHelpers.DeleteItemAsync(factory.Services, first.Id);
            await PantryTestHelpers.DeleteItemAsync(factory.Services, second.Id);
        }
    }

    [Theory]
    [InlineData("status=bogus", "Status")]
    [InlineData("sort=bogus", "Sort")]
    public async Task InvalidQuery_ReturnsValidationProblem(string query, string field)
    {
        var response = await _client.GetAsync($"/api/pantry?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task NotificationsPaging_ReturnsSliceAndTotal()
    {
        var prefix = $"test-npage-{Guid.CreateVersion7()}";
        var ids = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var item = await PantryTestHelpers.SeedItemAsync(factory.Services, $"{prefix}-{i}", DateOnly.FromDateTime(DateTime.Today.AddDays(1)));
            ids.Add(item.Id);
            await SeedNotificationAsync(item.Id, item.ExpirationDate);
        }

        try
        {
            var page = await _client.GetFromJsonAsync<GetNotificationsResponse>(
                "/api/notifications?skip=1&take=1");

            Assert.NotNull(page);
            Assert.Single(page.Items);
            Assert.Equal(3, page.TotalCount);
            Assert.Equal(3, page.UnreadCount);
        }
        finally
        {
            foreach (var id in ids)
                await PantryTestHelpers.DeleteItemAsync(factory.Services, id);
        }
    }

    private async Task SeedNotificationAsync(string itemId, DateOnly expiration)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Notifications.Add(new Notification
        {
            Type = "expiry-warning",
            PantryItemId = itemId,
            ExpirationDate = expiration
        });
        await db.SaveChangesAsync();
    }

}