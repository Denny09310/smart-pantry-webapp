using Microsoft.EntityFrameworkCore;

using Npgsql;

using Server.Data;
using Server.Data.Entities;

using Shared.Models;

namespace Server.Services;

/// <summary>
/// Daily expiry reminder check: one in-app notification per item entering the
/// 7-day window, plus a best-effort push for each new reminder. Idempotent via
/// the (item, expiry date, type) identity, so extra runs — including the
/// startup catch-up — never duplicate reminders. A single item failure never
/// stops the rest, and one failed push never blocks the others.
/// </summary>
public sealed class ReminderWorker(
    IServiceScopeFactory scopes,
    IPushSender push,
    ILogger<ReminderWorker> log) : BackgroundService
{
    public const string ExpiryWarningType = "expiry-warning";
    public const int ReminderDaysAhead = CosmeticRules.ExpirySoonDays;

    /// <summary>Server-local time of the daily run.</summary>
    private static readonly TimeSpan RunAt = new(8, 0, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCheckAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(NextRunDelay(DateTime.Now), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunCheckAsync(stoppingToken);
        }
    }

    /// <summary>
    /// One guarded run: a DB outage or transient failure must never kill the
    /// worker — the next daily run simply tries again.
    /// </summary>
    private async Task RunCheckAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CheckAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Reminder check failed; will retry on the next run.");
        }
    }

    public static TimeSpan NextRunDelay(DateTime now)
    {
        var today = now.Date + RunAt;
        var next = now < today ? today : today.AddDays(1);
        return next - now;
    }

    public async Task<ReminderResult> CheckAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Server-local day: the household server defines "today".
        var today = DateOnly.FromDateTime(DateTime.Today);
        var horizon = today.AddDays(ReminderDaysAhead);

        log.LogInformation("Reminder check started (today {Today}, horizon {Horizon}).", today, horizon);

        var candidates = await db.Items.AsNoTracking()
            .Where(x => x.ExpiryDate >= today && x.ExpiryDate <= horizon)
            .Select(x => new { x.Id, x.Name, x.ExpiryDate })
            .ToListAsync(ct);

        // Only reminders for items in the current window matter: the table
        // grows forever, but each check reads just this slice.
        var candidateIds = candidates.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);

        var seen = (await db.Notifications.AsNoTracking()
            .Where(x => x.Type == ExpiryWarningType && candidateIds.Contains(x.CosmeticItemId))
            .Select(x => new { x.CosmeticItemId, x.ExpiryDate })
            .ToListAsync(ct))
            .Select(x => (x.CosmeticItemId, x.ExpiryDate))
            .ToHashSet();

        var created = 0;
        var failed = 0;
        var reminded = new List<(string Id, string Name, DateOnly ExpiryDate)>();

        foreach (var item in candidates)
        {
            if (!seen.Add((item.Id, item.ExpiryDate)))
                continue;

            try
            {
                db.Notifications.Add(new Notification
                {
                    Type = ExpiryWarningType,
                    CosmeticItemId = item.Id,
                    ExpiryDate = item.ExpiryDate
                });
                await db.SaveChangesAsync(ct);
                created++;
                reminded.Add((item.Id, item.Name, item.ExpiryDate));
            }
            catch (OperationCanceledException)
            {
                // Shutting down: stop at once instead of failing every rest item.
                db.ChangeTracker.Clear();
                throw;
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                // Lost a race with an overlapping run: the reminder already exists.
                db.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                failed++;
                log.LogWarning(ex, "Reminder for item {ItemId} failed; continuing with the rest.", item.Id);
                db.ChangeTracker.Clear();
            }
        }

        var push = await SendPushAsync(db, reminded, ct);

        log.LogInformation(
            "Reminder check finished: {Candidates} candidates, {Created} created, {Failed} failed, " +
            "{PushSent} pushes sent, {PushInvalid} invalid subscriptions removed.",
            candidates.Count, created, failed, push.Sent, push.Invalid);

        return new ReminderResult(candidates.Count, created, failed, push.Sent, push.Invalid);
    }

    private async Task<(int Sent, int Invalid)> SendPushAsync(
        ApplicationDbContext db,
        List<(string Id, string Name, DateOnly ExpiryDate)> reminded,
        CancellationToken ct)
    {
        if (reminded.Count == 0)
            return (0, 0);

        var subscriptions = await db.PushSubscriptions.AsNoTracking().ToListAsync(ct);
        if (subscriptions.Count == 0)
        {
            // Reminders exist but nobody can receive them. Silence here reads
            // exactly like "nothing to send", which is how a household ends up
            // with in-app reminders and no push with no clue why.
            log.LogInformation(
                "{Count} reminder(s) created but no push subscription is registered; " +
                "push delivery skipped. Enable notifications in the app to receive them.",
                reminded.Count);

            return (0, 0);
        }

        var sent = 0;
        var notConfigured = 0;
        var invalidIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in reminded)
        {
            var title = "Expiring soon";
            var body = $"{item.Name} expires {item.ExpiryDate:MMM d}.";

            foreach (var subscription in subscriptions)
            {
                // Already known dead this run: skip without recounting.
                if (invalidIds.Contains(subscription.Id))
                    continue;

                PushSendOutcome outcome;
                try
                {
                    outcome = await push.TrySendAsync(new WebPushSenderRequest(
                        subscription.Endpoint, subscription.P256dh, subscription.Auth,
                        title, body, "/"), ct);
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Push to {Endpoint} failed.", subscription.Endpoint);
                    continue;
                }

                if (outcome == PushSendOutcome.Delivered)
                {
                    sent++;
                }
                else if (outcome == PushSendOutcome.InvalidSubscription)
                {
                    invalidIds.Add(subscription.Id);
                    log.LogInformation("Removing invalid push subscription {Id}.", subscription.Id);
                }
                else if (outcome == PushSendOutcome.NotConfigured)
                {
                    notConfigured++;
                }
            }
        }

        // A missing VAPID key pair fails every delivery identically and silently:
        // reminders pile up in-app while nothing ever reaches the device.
        if (notConfigured > 0)
        {
            log.LogWarning(
                "{Count} push delivery attempt(s) skipped because VAPID keys are not configured. " +
                "Set Vapid:PublicKey and Vapid:PrivateKey (plus Vapid:Subject) on the server.",
                notConfigured);
        }

        // One delete for all dead endpoints, after every delivery attempt.
        if (invalidIds.Count > 0)
        {
            try
            {
                await db.PushSubscriptions
                    .Where(x => invalidIds.Contains(x.Id))
                    .ExecuteDeleteAsync(ct);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Removing invalid push subscriptions failed.");
            }
        }

        return (sent, invalidIds.Count);
    }

    public sealed record ReminderResult(int Candidates, int Created, int Failed, int PushSent, int PushInvalid);
}