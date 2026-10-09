using System.Net;
using System.Net.Http.Json;

using Shared.Models;

namespace Server.Tests;

[Collection("api")]
public sealed class PushEndpointsTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueEndpoint()
        => $"https://push.example.com/sub/{Guid.NewGuid():N}";

    [Fact]
    public async Task PublicKey_Returns_Configured_Value()
    {
        using var response = await _client.GetAsync("/api/push/vapid-public-key");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var key = await response.Content.ReadFromJsonAsync<PushPublicKeyResponse>();
        Assert.NotNull(key);
        Assert.False(string.IsNullOrWhiteSpace(key.PublicKey));

        // Uncompressed P-256 point: 65 bytes base64url-encoded.
        var padded = key.PublicKey.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        Assert.Equal(65, Convert.FromBase64String(padded).Length);
    }

    [Fact]
    public async Task Subscribe_Is_Idempotent_Then_Unsubscribe_Removes()
    {
        var endpoint = UniqueEndpoint();
        var request = new PushSubscriptionRequest(endpoint, "c256dh-test", "auth-test");

        try
        {
            using var first = await _client.PostAsJsonAsync("/api/push/subscriptions", request);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var saved = await first.Content.ReadFromJsonAsync<PushSubscriptionDto>();
            Assert.NotNull(saved);
            Assert.Equal(endpoint, saved.Endpoint);

            using var second = await _client.PostAsJsonAsync("/api/push/subscriptions", request);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var again = await second.Content.ReadFromJsonAsync<PushSubscriptionDto>();
            Assert.NotNull(again);
            Assert.Equal(saved.Id, again.Id);
        }
        finally
        {
            using var removed = await _client.DeleteAsync($"/api/push/subscriptions?endpoint={Uri.EscapeDataString(endpoint)}");
            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        }

        using var gone = await _client.DeleteAsync($"/api/push/subscriptions?endpoint={Uri.EscapeDataString(endpoint)}");
        Assert.Equal(HttpStatusCode.NoContent, gone.StatusCode);
    }

    [Fact]
    public async Task Subscribe_Rejects_Empty_Subscription()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/push/subscriptions",
            new PushSubscriptionRequest(string.Empty, string.Empty, string.Empty));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_Requires_Endpoint()
    {
        using var response = await _client.DeleteAsync("/api/push/subscriptions");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}