using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Server.Data;
using WebPush;

namespace Server.Services;

public sealed class PushOptions
{
    public const string SectionName = "Vapid";

    public string Subject { get; set; } = "mailto:smart-pantry@example.com";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}

/// <summary>
/// Server half of Web Push: signs with the VAPID private key and POSTs
/// encrypted payloads to browser endpoints via the WebPush library.
/// </summary>
internal sealed class PushService(IOptions<PushOptions> options, ApplicationDbContext db)
{
    private readonly PushOptions _options = options.Value;

    /// <summary>
    /// Sends every unread notification to every stored subscription.
    /// Subscriptions the push service reports as gone (404/410) are removed.
    /// Returns how many pushes were accepted.
    /// </summary>
    public async Task<int> SendUnreadAsync(CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("VAPID keys are not configured (Push:Vapid section).");

        var messages = await db.Notifications.AsNoTracking()
            .Where(n => n.ReadAt == null)
            .Select(n => n.Message)
            .Distinct()
            .ToListAsync(ct);

        if (messages.Count == 0)
            return 0;

        var subscriptions = await db.PushSubscriptions.AsNoTracking().ToListAsync(ct);

        if (subscriptions.Count == 0)
            return 0;

        var vapid = new VapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
        using var client = new WebPushClient();

        var sent = 0;
        var dead = new List<string>();

        foreach (var subscription in subscriptions)
        {
            ct.ThrowIfCancellationRequested();

            var target = new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);

            // One push per subscription is enough to wake the client;
            // the panel lists every unread notification.
            foreach (var message in messages.Take(1))
            {
                try
                {
                    await client.SendNotificationAsync(target, message, vapid);
                    sent++;
                }
                catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    dead.Add(subscription.Id);
                    break;
                }
            }
        }

        if (dead.Count > 0)
        {
            await db.PushSubscriptions
                .Where(s => dead.Contains(s.Id))
                .ExecuteDeleteAsync(ct);
        }

        return sent;
    }
}
