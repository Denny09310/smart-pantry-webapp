using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Data.Entities;
using Shared.Models;

namespace Server.Workers;

/// <summary>
/// Daily expiry reminder check: one in-app notification per item entering the
/// 7-day window. Idempotent via the (item, expiry date, type) identity, so
/// extra runs — including the startup catch-up — never duplicate reminders.
/// A single item failure never stops the rest.
/// </summary>
public sealed class ReminderWorker(IServiceScopeFactory scopes, ILogger<ReminderWorker> log) : BackgroundService
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
            .Select(x => new { x.Id, x.ExpirationDate })
            .ToListAsync(ct);

        var seen = (await db.Notifications.AsNoTracking()
            .Where(x => x.Type == ExpiryWarningType)
            .Select(x => new { x.PantryItemId, x.ExpirationDate })
            .ToListAsync(ct))
            .Select(x => (x.PantryItemId, x.ExpirationDate))
            .ToHashSet();

        var created = 0;
        var failed = 0;

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
            }
            catch (Exception ex)
            {
                failed++;
                log.LogWarning(ex, "Reminder for item {ItemId} failed; continuing with the rest.", item.Id);
                db.ChangeTracker.Clear();
            }
        }

        log.LogInformation(
            "Reminder check finished: {Candidates} candidates, {Created} created, {Failed} failed.",
            candidates.Count, created, failed);

        return new ReminderResult(candidates.Count, created, failed);
    }

    public sealed record ReminderResult(int Candidates, int Created, int Failed);
}
