using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using Server.Data;
using Server.Data.Entities;
using Server.Services;

namespace Server.Tests;

public class NotificationApiTests(CosmeticApiFactory factory) : IClassFixture<CosmeticApiFactory>
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
        var item = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-item-{Guid.CreateVersion7()}");
        var notification = await SeedNotificationAsync(item.Id, item.ExpiryDate);

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
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, item.Id);
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
        var first = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-item-a-{Guid.CreateVersion7()}");
        var second = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-item-b-{Guid.CreateVersion7()}");
        await SeedNotificationAsync(first.Id, first.ExpiryDate);
        await SeedNotificationAsync(second.Id, second.ExpiryDate);

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
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, first.Id);
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, second.Id);
        }
    }

    private async Task<Notification> SeedNotificationAsync(string itemId, DateOnly expiration)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = new Notification
        {
            Type = ReminderWorker.ExpiryWarningType,
            CosmeticItemId = itemId,
            ExpiryDate = expiration
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return notification;
    }


}

public class ReminderWorkerTests(CosmeticApiFactory factory) : IClassFixture<CosmeticApiFactory>
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
        var soon = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-soon-{Guid.CreateVersion7()}", today.AddDays(3));
        var todayItem = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-today-{Guid.CreateVersion7()}", today);
        var edge = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-edge-{Guid.CreateVersion7()}", today.AddDays(7));
        var expired = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-expired-{Guid.CreateVersion7()}", today.AddDays(-1));
        var future = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-future-{Guid.CreateVersion7()}", today.AddDays(8));

        try
        {
            var worker = GetWorker();

            // The hosted worker also runs CheckAsync in the background and may
            // claim some of these rows first, so drive to quiescence instead of
            // asserting exact per-run counts.
            for (var i = 0; i < 10; i++)
            {
                var run = await worker.CheckAsync(CancellationToken.None);
                Assert.Equal(0, run.Failed);
                if (run.Created == 0)
                    break;
            }

            // Exactly the in-window items were reminded, nothing else.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var remindedIds = await db.Notifications.AsNoTracking()
                    .Where(x => x.Type == ReminderWorker.ExpiryWarningType
                        && (x.CosmeticItemId == soon.Id
                            || x.CosmeticItemId == todayItem.Id
                            || x.CosmeticItemId == edge.Id
                            || x.CosmeticItemId == expired.Id
                            || x.CosmeticItemId == future.Id))
                    .Select(x => x.CosmeticItemId)
                    .ToListAsync();

                Assert.Equal(3, remindedIds.Count);
                Assert.Contains(soon.Id, remindedIds);
                Assert.Contains(todayItem.Id, remindedIds);
                Assert.Contains(edge.Id, remindedIds);
            }

            // A further run creates nothing: reminders are deduplicated.
            var steady = await worker.CheckAsync(CancellationToken.None);
            Assert.Equal(0, steady.Created);
            Assert.Equal(0, steady.Failed);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, soon.Id);
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, todayItem.Id);
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, edge.Id);
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, expired.Id);
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, future.Id);
        }
    }

    private ReminderWorker GetWorker()
    {
        using var scope = factory.Services.CreateScope();
        return new ReminderWorker(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            scope.ServiceProvider.GetRequiredService<IPushSender>(),
            NullLogger<ReminderWorker>.Instance);
    }


}