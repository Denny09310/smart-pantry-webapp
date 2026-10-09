using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using Server.Data;

using Shared.Models;

namespace Server.Endpoints;

[Tags("Notifications")]
[MapGroup("/api/notifications")]
internal class NotificationEndpoints(ApplicationDbContext db)
{
    [MapGet("/")]
    public async Task<Ok<GetNotificationsResponse>> GetNotificationsAsync(CancellationToken ct)
    {
        var items = await db.Notifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Join(db.Items.AsNoTracking(),
                n => n.PantryItemId,
                i => i.Id,
                (n, i) => new NotificationDto(
                    n.Id,
                    n.PantryItemId,
                    i.Name,
                    n.Message,
                    n.CreatedAt,
                    n.ReadAt))
            .Take(100)
            .ToListAsync(ct);

        var unread = await db.Notifications.AsNoTracking()
            .Where(n => n.ReadAt == null)
            .CountAsync(ct);

        return TypedResults.Ok(new GetNotificationsResponse(items, unread));
    }

    [MapPost("/{id}/read")]
    public async Task<Results<Ok<NotificationDto>, NotFound>> MarkReadAsync(
        string id,
        CancellationToken ct)
    {
        var entry = await db.Notifications.FindAsync([id], ct);

        if (entry is null)
            return TypedResults.NotFound();

        entry.ReadAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var itemName = await db.Items.AsNoTracking()
            .Where(i => i.Id == entry.PantryItemId)
            .Select(i => i.Name)
            .FirstOrDefaultAsync(ct);

        return TypedResults.Ok(new NotificationDto(
            entry.Id,
            entry.PantryItemId,
            itemName ?? string.Empty,
            entry.Message,
            entry.CreatedAt,
            entry.ReadAt));
    }

    [MapPost("/read-all")]
    public async Task<Ok<int>> MarkAllReadAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var marked = await db.Notifications
            .Where(n => n.ReadAt == null)
            .ExecuteUpdateAsync(n => n.SetProperty(x => x.ReadAt, now), ct);

        return TypedResults.Ok(marked);
    }
}