using System.Globalization;
using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

using Server.Data;

using Shared.Resources;

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
internal sealed class PushService(
    IOptions<PushOptions> options,
    ApplicationDbContext db,
    IStringLocalizer<UIStrings> localizer,
    ILogger<PushService> log)
{
    private readonly PushOptions _options = options.Value;

    public sealed record PushResult(int Sent, int Failed, int Pruned);

    /// <summary>
    /// Sends every unread notification to every stored subscription, each
    /// payload formatted in the subscription's language. One failing
    /// subscription never blocks the rest; subscriptions the push service
    /// reports as gone (404/410) are removed.
    /// </summary>
    public async Task<PushResult> SendUnreadAsync(CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("VAPID keys are not configured (Vapid section).");

        var pending = await db.Notifications.AsNoTracking()
            .Where(n => n.ReadAt == null)
            .OrderBy(n => n.CreatedAt)
            .Join(db.Items.AsNoTracking(),
                n => n.PantryItemId,
                i => i.Id,
                (n, i) => new { i.Name, i.ExpirationDate })
            .ToListAsync(ct);

        if (pending.Count == 0)
            return new PushResult(0, 0, 0);

        var subscriptions = await db.PushSubscriptions.AsNoTracking().ToListAsync(ct);

        if (subscriptions.Count == 0)
            return new PushResult(0, 0, 0);

        var vapid = new VapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
        using var client = new WebPushClient();

        // One push per subscription is enough to wake the client;
        // the panel lists every unread notification.
        var first = pending[0];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sent = 0;
        var failed = 0;
        var dead = new List<string>();

        foreach (var group in subscriptions.GroupBy(s => UIStrings.Normalize(s.Language)))
        {
            ct.ThrowIfCancellationRequested();

            // The localizer resolves the culture at call time; scope it to
            // this language group and restore afterwards (async-local).
            var previousCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = new CultureInfo(group.Key);

            string message;

            try
            {
                message = NotificationText.Format(first.Name, first.ExpirationDate, today, localizer);
            }
            finally
            {
                CultureInfo.CurrentUICulture = previousCulture;
            }

            foreach (var subscription in group)
            {
                ct.ThrowIfCancellationRequested();

                var target = new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);

                try
                {
                    await client.SendNotificationAsync(target, message, vapid);
                    sent++;
                }
                catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    dead.Add(subscription.Id);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    failed++;
                    log.LogWarning(ex, "Push to {Endpoint} failed.", subscription.Endpoint);
                }
            }
        }

        if (dead.Count > 0)
        {
            await db.PushSubscriptions
                .Where(s => dead.Contains(s.Id))
                .ExecuteDeleteAsync(ct);
        }

        return new PushResult(sent, failed, dead.Count);
    }
}