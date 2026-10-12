using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Data.Entities;
using Server.Push;
using Shared.Models;

namespace Server.Workers;

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
    public const int ReminderDaysAhead = PantryRules.ExpirySoonDays;

    /// <summary>Server-local time of the daily run.</summary>
    private static readonly TimeSpan RunAt = new(8, 0, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CheckAsync(stoppingToken);

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

            await CheckAsync(stoppingToken);
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
            .Where(x => x.ExpirationDate >= today && x.ExpirationDate <= horizon)
            .Select(x => new { x.Id, x.Name, x.ExpirationDate })
            .ToListAsync(ct);

        var seen = (await db.Notifications.AsNoTracking()
            .Where(x => x.Type == ExpiryWarningType)
            .Select(x => new { x.PantryItemId, x.ExpirationDate })
            .ToListAsync(ct))
            .Select(x => (x.PantryItemId, x.ExpirationDate))
            .ToHashSet();

        var created = 0;
        var failed = 0;
        var reminded = new List<(string Id, string Name, DateOnly ExpirationDate)>();

        foreach (var item in candidates)
        {
            if (!seen.Add((item.Id, item.ExpirationDate)))
                continue;

            try
            {
                db.Notifications.Add(new Notification
                {
                    Type = ExpiryWarningType,
                    PantryItemId = item.Id,
                    ExpirationDate = item.ExpirationDate
                });
                await db.SaveChangesAsync(ct);
                created++;
                reminded.Add((item.Id, item.Name, item.ExpirationDate));
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
        List<(string Id, string Name, DateOnly ExpirationDate)> reminded,
        CancellationToken ct)
    {
        if (reminded.Count == 0)
            return (0, 0);

        var subscriptions = await db.PushSubscriptions.AsNoTracking().ToListAsync(ct);
        if (subscriptions.Count == 0)
            return (0, 0);

        var sent = 0;
        var invalid = 0;

        foreach (var item in reminded)
        {
            var title = "Expiring soon";
            var body = $"{item.Name} expires {item.ExpirationDate:MMM d}.";

            foreach (var subscription in subscriptions)
            {
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
                    invalid++;
                    log.LogInformation("Removing invalid push subscription {Id}.", subscription.Id);
                    await db.PushSubscriptions
                        .Where(x => x.Id == subscription.Id)
                        .ExecuteDeleteAsync(ct);
                }
            }
        }

        return (sent, invalid);
    }

    public sealed record ReminderResult(int Candidates, int Created, int Failed, int PushSent, int PushInvalid);
}
