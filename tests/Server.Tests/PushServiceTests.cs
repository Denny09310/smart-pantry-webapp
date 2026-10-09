using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;
using Server.Services;

using Shared.Models;

namespace Server.Tests;

[Collection("api")]
public sealed class PushServiceTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SendUnread_Tolerates_Failing_Subscriptions()
    {
        const string endpoint = "http://127.0.0.1:9/dead-subscription";

        using var subscribed = await _client.PostAsJsonAsync(
            "/api/push/subscriptions",
            new PushSubscriptionRequest(endpoint, "broken-key", "broken-secret"));
        subscribed.EnsureSuccessStatusCode();

        var itemId = await CreateExpiringItemAsync();

        try
        {
            await GenerateAsync();

            using var scope = factory.Services.CreateScope();
            var push = scope.ServiceProvider.GetRequiredService<PushService>();

            // Connection refused (or undecryptable keys): no throw, nothing sent.
            Assert.Equal(0, (await push.SendUnreadAsync()).Sent);

            // Transient failures keep the subscription for the next run.
            Assert.True(await scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>()
                .PushSubscriptions
                .AnyAsync(s => s.Endpoint == endpoint));
        }
        finally
        {
            await _client.DeleteAsync($"/api/pantry/{itemId}");
            await _client.DeleteAsync($"/api/push/subscriptions?endpoint={Uri.EscapeDataString(endpoint)}");
        }
    }

    [Fact]
    public async Task SendUnread_Prunes_Gone_Subscriptions()
    {
        using var listener = new HttpListener();
        var prefix = $"http://localhost:{FreePort()}/push/";
        listener.Prefixes.Add(prefix);
        listener.Start();

        var serving = ServeGoneAsync(listener);

        try
        {
            var endpoint = prefix + "dead";
            var (p256dh, auth) = SubscriberKeys();

            using var subscribed = await _client.PostAsJsonAsync(
                "/api/push/subscriptions",
                new PushSubscriptionRequest(endpoint, p256dh, auth));
            subscribed.EnsureSuccessStatusCode();

            var itemId = await CreateExpiringItemAsync();

            try
            {
                await GenerateAsync();

                using var scope = factory.Services.CreateScope();
                var push = scope.ServiceProvider.GetRequiredService<PushService>();

                Assert.Equal(0, (await push.SendUnreadAsync()).Sent);

                Assert.False(await scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>()
                    .PushSubscriptions
                    .AnyAsync(s => s.Endpoint == endpoint));
            }
            finally
            {
                await _client.DeleteAsync($"/api/pantry/{itemId}");
            }
        }
        finally
        {
            listener.Stop();
            await serving;
        }
    }

    private static async Task ServeGoneAsync(HttpListener listener)
    {
        try
        {
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                context.Response.StatusCode = (int)HttpStatusCode.Gone;
                context.Response.Close();
            }
        }
        catch (HttpListenerException) { }
        catch (ObjectDisposedException) { }
    }

    private static int FreePort()
    {
        using var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        socket.Start();
        return ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
    }

    private static (string p256dh, string auth) SubscriberKeys()
    {
        using var key = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(true);

        var uncompressed = new byte[65];
        uncompressed[0] = 0x04;
        Buffer.BlockCopy(parameters.Q.X!, 0, uncompressed, 1, 32);
        Buffer.BlockCopy(parameters.Q.Y!, 0, uncompressed, 33, 32);

        return (
            Convert.ToBase64String(uncompressed).Replace('+', '-').Replace('/', '_').TrimEnd('='),
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).Replace('+', '-').Replace('/', '_').TrimEnd('='));
    }

    private async Task<string> CreateExpiringItemAsync()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/pantry",
            new CreatePantryItemRequest(
                $"pushable-{Guid.NewGuid():N}",
                1,
                "pcs",
                "pantry",
                null,
                DateOnly.FromDateTime(DateTime.Today).AddDays(1)));
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(created);
        return created.Id;
    }

    private async Task GenerateAsync()
    {
        using var scope = factory.Services.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();
        Assert.True((await notifications.GenerateExpirationNotificationsAsync()).Created >= 1);
    }
}
