using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Data.Entities;
using Server.Hubs;

using Shared.Models;

namespace Server.Endpoints;

[Tags("Pantry")]
[MapGroup("/api/pantry")]
internal class PantryEndpoints(ApplicationDbContext db, IHubContext<PantryHub, IPantryHubClient> hub)
{
    [MapGet("/")]
    public async Task<Ok<GetPantryItemsResponse>> GetPantryItemsAsync(
        [AsParameters] GetPantryItemsRequest request,
        CancellationToken ct)
    {
        var query = db.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Location)
            && !request.Location.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => EF.Functions.ILike(x.Location, $"%{request.Location.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{request.Name.Trim()}%"));

        if (request.Status is { } status)
        {
            // UTC day everywhere: server local midnight must not shift boundaries.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var horizon = today.AddDays(7);
            query = status switch
            {
                ExpiryStatus.Expired => query.Where(x => x.ExpirationDate < today),
                ExpiryStatus.Today => query.Where(x => x.ExpirationDate == today),
                ExpiryStatus.Soon => query.Where(x => x.ExpirationDate > today && x.ExpirationDate <= horizon),
                ExpiryStatus.Fresh => query.Where(x => x.ExpirationDate > horizon),
                _ => query,
            };
        }
        else if (request.ExcludeFresh)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var horizon = today.AddDays(7);
            query = query.Where(x => x.ExpirationDate <= horizon);
        }

        var totalItems = await query.CountAsync(ct);

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        // Status is calculated in memory: the shared rule is not translatable to SQL.
        var ordered = request.SortDescending
            ? query.OrderByDescending(x => x.ExpirationDate).ThenByDescending(x => x.Id)
            : query.OrderBy(x => x.ExpirationDate).ThenBy(x => x.Id);

        var entries = await ordered
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        var memberIds = entries
            .Select(x => x.CreatedByMemberId)
            .Where(id => id != null)
            .Distinct()
            .ToList();

        var memberNames = memberIds.Count == 0
            ? new Dictionary<string, string>()
            : await db.Members.AsNoTracking()
                .Where(m => memberIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        var items = entries
            .Select(e => ToDto(e, e.CreatedByMemberId is not null && memberNames.TryGetValue(e.CreatedByMemberId, out var name) ? name : null))
            .ToList();

        return TypedResults.Ok(new GetPantryItemsResponse(
            items,
            totalItems));
    }

    [MapGet("/summary")]
    public async Task<Ok<GetPantrySummaryResponse>> GetPantrySummaryAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(7);

        var expired = await db.Items.CountAsync(x => x.ExpirationDate < today, ct);
        var todayCount = await db.Items.CountAsync(x => x.ExpirationDate == today, ct);
        var soon = await db.Items.CountAsync(x => x.ExpirationDate > today && x.ExpirationDate <= horizon, ct);
        var fresh = await db.Items.CountAsync(x => x.ExpirationDate > horizon, ct);

        var byLocation = await db.Items
            .AsNoTracking()
            .GroupBy(x => x.Location)
            .Select(g => new { Location = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Location, x => x.Count, ct);

        var total = expired + todayCount + soon + fresh;

        return TypedResults.Ok(new GetPantrySummaryResponse(
            total,
            expired,
            todayCount,
            soon,
            fresh,
            expired + todayCount + soon,
            byLocation));
    }

    [MapGet("/{id}")]
    public async Task<Results<Ok<PantryItemDto>, NotFound>> GetPantryItemAsync(
        string id,
        CancellationToken ct)
    {
        var entry = await db.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entry is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(ToDto(entry, await MemberNameAsync(entry.CreatedByMemberId, ct)));
    }

    [MapPost("/")]
    public async Task<Results<Created<PantryItemDto>, ValidationProblem>> CreatePantryItemAsync(
        CreatePantryItemRequest request,
        [FromHeader(Name = "X-Member-Id")] string? memberId,
        CancellationToken ct)
    {
        var entry = new PantryItem
        {
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            Unit = request.Unit.Trim(),
            Location = request.Location.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ExpirationDate = request.ExpirationDate,
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim(),
            CreatedByMemberId = await ResolveMemberIdAsync(memberId, ct)
        };

        db.Items.Add(entry);
        await db.SaveChangesAsync(ct);
        await hub.Clients.All.PantryChanged(new PantryChangedEvent(PantryChangedEvent.Created, entry.Id));

        return TypedResults.Created(
            $"/api/pantry/{entry.Id}",
            ToDto(entry, await MemberNameAsync(entry.CreatedByMemberId, ct)));
    }

    [MapPut("/{id}")]
    public async Task<Results<Ok<PantryItemDto>, NotFound, ValidationProblem>> UpdatePantryItemAsync(
        string id,
        UpdatePantryItemRequest request,
        CancellationToken ct)
    {
        // Attribution intentionally untouched: it records who added the item.
        var entry = await db.Items.FindAsync([id], ct);

        if (entry is null)
            return TypedResults.NotFound();

        entry.Name = request.Name.Trim();
        entry.Quantity = request.Quantity;
        entry.Unit = request.Unit.Trim();
        entry.Location = request.Location.Trim();
        entry.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        entry.ExpirationDate = request.ExpirationDate;
        entry.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        await hub.Clients.All.PantryChanged(new PantryChangedEvent(PantryChangedEvent.Updated, entry.Id));

        return TypedResults.Ok(ToDto(entry, await MemberNameAsync(entry.CreatedByMemberId, ct)));
    }

    [MapDelete("/{id}")]
    public async Task<Results<NoContent, NotFound>> DeletePantryItemAsync(
        string id,
        CancellationToken ct)
    {
        var entry = await db.Items.FindAsync([id], ct);

        if (entry is null)
            return TypedResults.NotFound();

        db.Items.Remove(entry);
        await db.SaveChangesAsync(ct);
        await hub.Clients.All.PantryChanged(new PantryChangedEvent(PantryChangedEvent.Removed, entry.Id));

        return TypedResults.NoContent();
    }



    private static PantryItemDto ToDto(PantryItem entry, string? memberName = null)
        => new(
            entry.Id,
            entry.Name,
            entry.Quantity,
            entry.Unit,
            entry.Location,
            entry.Notes,
            entry.ExpirationDate,
            ExpiryStatusCalculator.GetStatus(entry.ExpirationDate, DateOnly.FromDateTime(DateTime.UtcNow)),
            entry.CreatedByMemberId,
            memberName,
            entry.Barcode);

    private async Task<string?> MemberNameAsync(string? memberId, CancellationToken ct)
        => memberId is null
            ? null
            : await db.Members
                .AsNoTracking()
                .Where(m => m.Id == memberId)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Attribution is a label, never access control: unknown or missing
    /// members resolve to null instead of failing the write.
    /// </summary>
    private async Task<string?> ResolveMemberIdAsync(string? memberId, CancellationToken ct)
        => string.IsNullOrWhiteSpace(memberId)
            ? null
            : await db.Members
                .AsNoTracking()
                .Where(m => m.Id == memberId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync(ct);
}