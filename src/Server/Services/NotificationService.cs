using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Data.Entities;

namespace Server.Services;

/// <summary>
/// Application-level service behind the expiration worker (roadmap Phase 8):
/// worker → service → DbContext. No queues or schedulers involved.
/// </summary>
internal sealed class NotificationService(ApplicationDbContext db)
{
    /// <summary>
    /// Creates one notification per item expiring within 7 days (or already
    /// expired) that has no unread notification yet. Returns how many were created.
    /// </summary>
    public async Task<int> GenerateExpirationNotificationsAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var horizon = today.AddDays(7);

        var candidates = await db.Items.AsNoTracking()
            .Where(i => i.ExpirationDate <= horizon)
            .Select(i => new { i.Id, i.Name, i.ExpirationDate })
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return 0;

        var alreadyNotified = new HashSet<string>(await db.Notifications.AsNoTracking()
            .Where(n => n.ReadAt == null)
            .Select(n => n.PantryItemId)
            .Distinct()
            .ToListAsync(ct));

        var now = DateTimeOffset.UtcNow;
        var created = 0;

        foreach (var candidate in candidates)
        {
            // One unread notification per item: prevents duplicates while the
            // user hasn't acknowledged the previous one.
            if (!alreadyNotified.Add(candidate.Id))
                continue;

            db.Notifications.Add(new Notification
            {
                PantryItemId = candidate.Id,
                Message = BuildMessage(candidate.Name, candidate.ExpirationDate, today),
                CreatedAt = now,
            });
            created++;
        }

        if (created > 0)
            await db.SaveChangesAsync(ct);

        return created;
    }

    internal static string BuildMessage(string name, DateOnly expirationDate, DateOnly today)
    {
        var days = expirationDate.DayNumber - today.DayNumber;
        return days switch
        {
            < 0 => $"{name} expired {(-days == 1 ? "yesterday" : $"{-days} days ago")}.",
            0 => $"{name} expires today.",
            1 => $"{name} expires tomorrow.",
            _ => $"{name} expires in {days} days.",
        };
    }
}