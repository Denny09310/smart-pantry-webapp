using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Server.Data;
using Server.Data.Entities;
using Server.Endpoints;
using Server.Models;
using Server.Services;

using Shared.Models;

namespace Server.Tests;

public class PushEndpointTests(CosmeticApiFactory factory) : IClassFixture<CosmeticApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public void PublicKey_Missing_ReturnsNotFound()
    {
        var options = Options.Create(new VapidOptions());

        var result = PushEndpoints.GetPushNotificationKeyAsync(options);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(404, problem.StatusCode);
    }

    [Fact]
    public void PublicKey_Present_ReturnsKey()
    {
        var options = Options.Create(new VapidOptions { PublicKey = "test-key" });

        var result = PushEndpoints.GetPushNotificationKeyAsync(options);

        var ok = Assert.IsType<Ok<GetPushNotificationKey>>(result.Result);
        Assert.Equal("test-key", ok.Value?.PublicKey);
    }

    [Fact]
    public async Task Subscribe_CreateThenUpdate_IsIdempotentByEndpoint()
    {
        var endpoint = $"https://test.push/sub-{Guid.CreateVersion7()}";

        try
        {
            var first = await _client.PostAsJsonAsync("/api/push/subscriptions",
                new PushSubscriptionRequest(endpoint, "p256dh-1", "auth-1"));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            var created = await first.Content.ReadFromJsonAsync<PushSubscriptionDto>();
            Assert.NotNull(created);
            Assert.NotEqual(default, created.CreatedAt);

            var second = await _client.PostAsJsonAsync("/api/push/subscriptions",
                new PushSubscriptionRequest(endpoint, "p256dh-2", "auth-2"));
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var updated = await second.Content.ReadFromJsonAsync<PushSubscriptionDto>();
            Assert.NotNull(updated);
            Assert.Equal(created.Id, updated.Id);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await db.PushSubscriptions.CountAsync(x => x.Endpoint == endpoint));
            Assert.Equal("p256dh-2", (await db.PushSubscriptions.SingleAsync(x => x.Endpoint == endpoint)).P256dh);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteSubscriptionAsync(factory.Services, endpoint);
        }
    }

    [Fact]
    public async Task Subscribe_InvalidBody_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/push/subscriptions",
            new PushSubscriptionRequest("", "", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("Endpoint", out _));
    }

    [Fact]
    public async Task Unsubscribe_BlankEndpoint_ReturnsValidationProblem()
    {
        var response = await _client.DeleteAsync("/api/push/subscriptions?endpoint=%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_Removes_AndIsIdempotent()
    {
        var endpoint = $"https://test.push/sub-{Guid.CreateVersion7()}";
        await _client.PostAsJsonAsync("/api/push/subscriptions",
            new PushSubscriptionRequest(endpoint, "p256dh", "auth"));

        var first = await _client.DeleteAsync($"/api/push/subscriptions?endpoint={Uri.EscapeDataString(endpoint)}");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await _client.DeleteAsync($"/api/push/subscriptions?endpoint={Uri.EscapeDataString(endpoint)}");
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

}

public class PushDeliveryTests(CosmeticApiFactory factory) : IClassFixture<CosmeticApiFactory>
{
    [Fact]
    public async Task CheckAsync_SendsPushForNewReminders()
    {
        var item = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-push-{Guid.CreateVersion7()}");
        var endpoint = $"https://test.push/sub-{Guid.CreateVersion7()}";
        await CosmeticTestHelpers.SeedSubscriptionAsync(factory.Services, endpoint);

        try
        {
            var sender = new RecordingSender(PushSendOutcome.Delivered);
            var worker = CreateWorker(sender);

            var result = await worker.CheckAsync(CancellationToken.None);

            Assert.Equal(1, result.Created);
            Assert.Equal(1, result.PushSent);
            var sent = Assert.Single(sender.Sent);
            Assert.Equal(endpoint, sent.Endpoint);
            Assert.Contains(item.Name, sent.Body);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, item.Id);
            await CosmeticTestHelpers.DeleteSubscriptionAsync(factory.Services, endpoint);
        }
    }

    [Fact]
    public async Task CheckAsync_RemovesInvalidSubscriptions()
    {
        var item = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-push-{Guid.CreateVersion7()}");
        var endpoint = $"https://test.push/sub-{Guid.CreateVersion7()}";
        await CosmeticTestHelpers.SeedSubscriptionAsync(factory.Services, endpoint);

        try
        {
            var sender = new RecordingSender(PushSendOutcome.InvalidSubscription);
            var worker = CreateWorker(sender);

            var result = await worker.CheckAsync(CancellationToken.None);

            Assert.Equal(1, result.Created);
            Assert.Equal(1, result.PushInvalid);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await db.PushSubscriptions.AnyAsync(x => x.Endpoint == endpoint));
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, item.Id);
            await CosmeticTestHelpers.DeleteSubscriptionAsync(factory.Services, endpoint);
        }
    }

    [Fact]
    public async Task CheckAsync_CountsDeadSubscriptionOnceAcrossItems()
    {
        var first = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-push-{Guid.CreateVersion7()}");
        var second = await CosmeticTestHelpers.SeedItemAsync(factory.Services, $"test-push-{Guid.CreateVersion7()}");
        var endpoint = $"https://test.push/sub-{Guid.CreateVersion7()}";
        await CosmeticTestHelpers.SeedSubscriptionAsync(factory.Services, endpoint);

        try
        {
            var sender = new RecordingSender(PushSendOutcome.InvalidSubscription);
            var worker = CreateWorker(sender);

            var result = await worker.CheckAsync(CancellationToken.None);

            Assert.Equal(2, result.Created);
            Assert.Equal(1, result.PushInvalid);
            Assert.Single(sender.Sent);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await db.PushSubscriptions.AnyAsync(x => x.Endpoint == endpoint));
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, first.Id);
            await CosmeticTestHelpers.DeleteItemAsync(factory.Services, second.Id);
            await CosmeticTestHelpers.DeleteSubscriptionAsync(factory.Services, endpoint);
        }
    }

    [Fact]
    public async Task WebPushSender_WithoutKeys_ReportsNotConfigured()
    {
        var sender = new WebPushSender(
            Microsoft.Extensions.Options.Options.Create(new VapidOptions()),
            NullLogger<WebPushSender>.Instance);

        var outcome = await sender.TrySendAsync(
            new WebPushSenderRequest("https://test.push/x", "p", "a", "t", "b", "/"),
            CancellationToken.None);

        Assert.Equal(PushSendOutcome.NotConfigured, outcome);
    }

    private ReminderWorker CreateWorker(IPushSender sender)
    {
        using var scope = factory.Services.CreateScope();
        var scopes = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        return new ReminderWorker(scopes, sender, NullLogger<ReminderWorker>.Instance);
    }

    private sealed class RecordingSender(PushSendOutcome outcome) : IPushSender
    {
        public readonly List<WebPushSenderRequest> Sent = [];
        private readonly PushSendOutcome _outcome = outcome;

        public Task<PushSendOutcome> TrySendAsync(WebPushSenderRequest request, CancellationToken ct)
        {
            Sent.Add(request);
            return Task.FromResult(_outcome);
        }
    }

}