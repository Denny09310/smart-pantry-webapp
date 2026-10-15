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
    public async Task<Ok<GetNotificationsResponse>> GetNotificationsAsync(
        [AsParameters] GetNotificationsRequest request,
        CancellationToken ct)
    {
        var items = await db.Notifications.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Join(db.Items.AsNoTracking(),
                n => n.PantryItemId,
                i => i.Id,
                (n, i) => new NotificationDto(
                    n.Id,
                    n.PantryItemId,
                    i.Name,
                    i.ExpirationDate,
                    n.CreatedAt,
                    n.ReadAt))
            .Skip(Math.Max(0, request.Skip ?? 0))
            .Take(Math.Clamp(request.Take ?? PantryRules.DefaultPageSize, 1, PantryRules.MaxPageSize))
            .ToListAsync(ct);

        var unread = await db.Notifications.AsNoTracking()
            .Where(x => x.ReadAt == null)
            .CountAsync(ct);

        var total = await db.Notifications.CountAsync(ct);

        return TypedResults.Ok(new GetNotificationsResponse(items, unread, total));
    }

    [MapPost("/{id}/read")]
    public async Task<Results<Ok<NotificationDto>, ProblemHttpResult>> MarkReadAsync(
        string id,
        CancellationToken ct)
    {
        var entry = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null)
            return NotificationNotFound(id);

        var item = await db.Items.AsNoTracking()
            .Where(x => x.Id == entry.PantryItemId)
            .Select(x => new { x.Name, x.ExpirationDate })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Pantry item '{entry.PantryItemId}' for notification '{id}' was not found.");

        entry.ReadAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new NotificationDto(
            entry.Id,
            entry.PantryItemId,
            item.Name,
            item.ExpirationDate,
            entry.CreatedAt,
            entry.ReadAt));
    }

    [MapPost("/read-all")]
    public async Task<Ok<int>> MarkAllReadAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var marked = await db.Notifications
            .Where(x => x.ReadAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(n => n.ReadAt, now), ct);

        return TypedResults.Ok(marked);
    }

    private static ProblemHttpResult NotificationNotFound(string id)
        => TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            detail: $"Notification '{id}' was not found.");
}