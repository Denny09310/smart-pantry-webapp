using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Data.Entities;
using Shared.Models;

namespace Server.Endpoints;

[Tags("Pantry")]
[MapGroup("/api/pantry")]
internal class PantryEndpoints(ApplicationDbContext db)
{
    [MapGet("/")]
    public async Task<Ok<GetPantryItemsResponse>> GetPantryItemsAsync(
        [AsParameters] GetPantryItemsRequest request,
        CancellationToken ct)
    {
        var query = db.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Location) && request.Location is not "all")
            query = query.Where(x => EF.Functions.ILike(x.Location, $"%{request.Location}%"));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{request.Name}%"));

        if (request.Status is { } status)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
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

        var totalItems = await query.CountAsync(ct);

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        // Status is calculated in memory: the shared rule is not translatable to SQL.
        var entries = await query
            .OrderBy(x => x.ExpirationDate)
            .ThenBy(x => x.Id)
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
    public async Task<Results<Ok<PantryItemDto>, ValidationProblem>> CreatePantryItemAsync(
        CreatePantryItemRequest request,
        CancellationToken ct)
    {
        var entry = new PantryItem
        {
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            Unit = request.Unit,
            Location = request.Location,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ExpirationDate = request.ExpirationDate
        };

        db.Items.Add(entry);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToDto(entry));
    }

    [MapPut("/{id}")]
    public async Task<Results<Ok<PantryItemDto>, NotFound, ValidationProblem>> UpdatePantryItemAsync(
        string id,
        UpdatePantryItemRequest request,
        CancellationToken ct)
    {
        var entry = await db.Items.FindAsync([id], ct);

        if (entry is null)
            return TypedResults.NotFound();

        entry.Name = request.Name.Trim();
        entry.Quantity = request.Quantity;
        entry.Unit = request.Unit;
        entry.Location = request.Location;
        entry.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        entry.ExpirationDate = request.ExpirationDate;
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToDto(entry));
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
            ExpiryStatusCalculator.GetStatus(entry.ExpirationDate, DateOnly.FromDateTime(DateTime.Today)),
            entry.CreatedByMemberId,
            memberName);

    private async Task<string?> MemberNameAsync(string? memberId, CancellationToken ct)
        => memberId is null
            ? null
            : await db.Members
                .AsNoTracking()
                .Where(m => m.Id == memberId)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct);
}
