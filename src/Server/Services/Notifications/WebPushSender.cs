using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Server.Models;

using WebPush;

namespace Server.Services;

public enum PushSendOutcome
{
    Delivered,
    InvalidSubscription,
    Failed,
    NotConfigured
}

public sealed record WebPushSenderRequest(
    string Endpoint,
    string P256dh,
    string Auth,
    string Title,
    string Body,
    string Url);

public interface IPushSender
{
    Task<PushSendOutcome> TrySendAsync(WebPushSenderRequest request, CancellationToken ct);
}

/// <summary>
/// Sends Web Push messages with the configured VAPID keys. Never throws:
/// a missing configuration reports <see cref="PushSendOutcome.NotConfigured"/>,
/// a rejected subscription <see cref="PushSendOutcome.InvalidSubscription"/>.
/// </summary>
public sealed class WebPushSender(IOptions<VapidOptions> options, ILogger<WebPushSender> log) : IPushSender
{
    public async Task<PushSendOutcome> TrySendAsync(WebPushSenderRequest request, CancellationToken ct)
    {
        var vapid = options.Value;

        if (!vapid.IsConfigured)
            return PushSendOutcome.NotConfigured;

        try
        {
            var subscription = new PushSubscription(request.Endpoint, request.P256dh, request.Auth);
            var details = new VapidDetails(vapid.Subject, vapid.PublicKey, vapid.PrivateKey);
            var payload = JsonSerializer.Serialize(new { request.Title, request.Body, request.Url });

            using var client = new WebPushClient();
            await client.SendNotificationAsync(subscription, payload, details, ct);

            return PushSendOutcome.Delivered;
        }
        catch (WebPushException ex) when (
            ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
        {
            log.LogInformation("Push subscription {Endpoint} rejected ({Status}); removing.", request.Endpoint, ex.StatusCode);
            return PushSendOutcome.InvalidSubscription;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Push to {Endpoint} failed.", request.Endpoint);
            return PushSendOutcome.Failed;
        }
    }
}