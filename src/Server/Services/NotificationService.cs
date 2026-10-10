using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Data.Entities;

namespace Server.Services;

/// <summary>
/// Application-level service behind the expiration worker:
/// worker → service → DbContext. No queues or schedulers involved.
/// </summary>
internal sealed class NotificationService(ApplicationDbContext db)
{
    public sealed record GenerationResult(int Created, int Evaluated);

    /// <summary>
    /// Creates one notification per item expiring within 7 days (or already
    /// expired) that was never notified before.
    /// </summary>
    public async Task<GenerationResult> GenerateExpirationNotificationsAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(7);

        var candidates = await db.Items.AsNoTracking()
            .Where(i => i.ExpirationDate <= horizon)
            .Select(i => new { i.Id, i.ExpirationDate })
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return new GenerationResult(0, 0);

        // One notification per item ever: a read reminder never regenerates.
        var alreadyNotified = new HashSet<string>(await db.Notifications.AsNoTracking()
            .Select(n => n.PantryItemId)
            .Distinct()
            .ToListAsync(ct));

        var now = DateTimeOffset.UtcNow;
        var created = 0;

        foreach (var candidate in candidates)
        {
            // One notification per item ever: prevents duplicates forever,
            // including after the previous one was read.
            if (!alreadyNotified.Add(candidate.Id))
                continue;

            db.Notifications.Add(new Notification
            {
                PantryItemId = candidate.Id,
                CreatedAt = now,
            });
            created++;
        }

        if (created > 0)
            await db.SaveChangesAsync(ct);

        return new GenerationResult(created, candidates.Count);
    }
}