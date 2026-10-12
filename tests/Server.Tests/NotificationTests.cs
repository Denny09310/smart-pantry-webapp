using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.Data;
using Server.Data.Entities;
using Server.Services;

namespace Server.Tests;

public class NotificationApiTests(PantryApiFactory factory) : IClassFixture<PantryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Empty_ReturnsEmptyListWithZeroUnread()
    {
        var response = await _client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Shared.Models.GetNotificationsResponse>();
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.UnreadCount);
    }

    [Fact]
    public async Task MarkRead_SetsReadAt_AndDecrementsUnread()
    {
        var item = await SeedItemAsync($"test-item-{Guid.CreateVersion7()}");
        var notification = await SeedNotificationAsync(item.Id, item.ExpirationDate);

        try
        {
            var read = await _client.PostAsync($"/api/notifications/{notification.Id}/read", null);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);

            var body = await read.Content.ReadFromJsonAsync<Shared.Models.NotificationDto>();
            Assert.NotNull(body);
            Assert.NotNull(body.ReadAt);
            Assert.Equal(item.Name, body.ItemName);

            var list = await _client.GetFromJsonAsync<Shared.Models.GetNotificationsResponse>("/api/notifications");
            Assert.NotNull(list);
            Assert.Equal(0, list.UnreadCount);
        }
        finally
        {
            await DeleteItemAsync(item.Id);
        }
    }

    [Fact]
    public async Task MarkRead_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/notifications/{Guid.CreateVersion7()}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_MarksEverything()
    {
        var first = await SeedItemAsync($"test-item-a-{Guid.CreateVersion7()}");
        var second = await SeedItemAsync($"test-item-b-{Guid.CreateVersion7()}");
        await SeedNotificationAsync(first.Id, first.ExpirationDate);
        await SeedNotificationAsync(second.Id, second.ExpirationDate);

        try
        {
            var response = await _client.PostAsync("/api/notifications/read-all", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var marked = await response.Content.ReadFromJsonAsync<int>();
            Assert.Equal(2, marked);

            var list = await _client.GetFromJsonAsync<Shared.Models.GetNotificationsResponse>("/api/notifications");
            Assert.NotNull(list);
            Assert.Equal(0, list.UnreadCount);
        }
        finally
        {
            await DeleteItemAsync(first.Id);
            await DeleteItemAsync(second.Id);
        }
    }

    private async Task<PantryItem> SeedItemAsync(string name)
    {
        using var scope = factory.Services.CreateScope();
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
        return item;
    }

    private async Task<Notification> SeedNotificationAsync(string itemId, DateOnly expiration)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = new Notification
        {
            Type = ReminderWorker.ExpiryWarningType,
            PantryItemId = itemId,
            ExpirationDate = expiration
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return notification;
    }

    private async Task DeleteItemAsync(string id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Items.Where(x => x.Id == id).ExecuteDeleteAsync();
    }
}

public class ReminderWorkerTests(PantryApiFactory factory) : IClassFixture<PantryApiFactory>
{
    [Theory]
    [InlineData(7, 0, 0, 1)] // 7am -> 1h to 8am
    [InlineData(9, 0, 0, 23)] // 9am -> next day
    [InlineData(8, 0, 0, 24)] // exactly 8am -> next day
    [InlineData(23, 30, 0, 8.5)]
    public void NextRunDelay_TargetsEightAm(int hour, int minute, int second, double expectedHours)
    {
        var now = DateOnly.FromDateTime(DateTime.Today).ToDateTime(new TimeOnly(hour, minute, second));
        var delay = ReminderWorker.NextRunDelay(now);

        Assert.True(Math.Abs((delay - TimeSpan.FromHours(expectedHours)).TotalSeconds) < 1);
    }

    [Fact]
    public async Task CheckAsync_CreatesRemindersOnlyInsideWindow_AndDeduplicates()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var soon = await SeedItemAsync($"test-soon-{Guid.CreateVersion7()}", today.AddDays(3));
        var todayItem = await SeedItemAsync($"test-today-{Guid.CreateVersion7()}", today);
        var edge = await SeedItemAsync($"test-edge-{Guid.CreateVersion7()}", today.AddDays(7));
        var expired = await SeedItemAsync($"test-expired-{Guid.CreateVersion7()}", today.AddDays(-1));
        var future = await SeedItemAsync($"test-future-{Guid.CreateVersion7()}", today.AddDays(8));

        try
        {
            var worker = GetWorker();

            var first = await worker.CheckAsync(CancellationToken.None);
            Assert.Equal(3, first.Created);
            Assert.Equal(0, first.Failed);

            var second = await worker.CheckAsync(CancellationToken.None);
            Assert.Equal(0, second.Created);
        }
        finally
        {
            await DeleteItemAsync(soon.Id);
            await DeleteItemAsync(todayItem.Id);
            await DeleteItemAsync(edge.Id);
            await DeleteItemAsync(expired.Id);
            await DeleteItemAsync(future.Id);
        }
    }

    private ReminderWorker GetWorker()
    {
        using var scope = factory.Services.CreateScope();
        var hosted = scope.ServiceProvider.GetServices<IHostedService>();
        return Assert.IsType<ReminderWorker>(hosted.OfType<ReminderWorker>().Single());
    }

    private async Task<PantryItem> SeedItemAsync(string name, DateOnly expiration)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var item = new PantryItem
        {
            Name = name,
            Quantity = 1,
            Unit = "pcs",
            Location = "pantry",
            ExpirationDate = expiration
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private async Task DeleteItemAsync(string id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Items.Where(x => x.Id == id).ExecuteDeleteAsync();
    }
}
