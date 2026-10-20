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

[Tags("Cosmetics")]
[MapGroup("/api/cosmetics")]
internal class CosmeticEndpoints(
    ApplicationDbContext db,
    IHubContext<CosmeticHub, ICosmeticHubClient> hub,
    ILogger<CosmeticEndpoints> log)
{
    [MapGet("/")]
    public async Task<Results<Ok<GetCosmeticItemsResponse>, ValidationProblem>> GetCosmeticItemsAsync(
        [AsParameters] GetCosmeticItemsRequest request,
        CancellationToken ct)
    {
        // "all" is documented as unfiltered: normalize before validating.
        var status = request.Status is CosmeticRules.StatusAll ? null : request.Status;

        if (status is not null && !CosmeticRules.Statuses.Contains(status))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Status)] = [$"Unknown status. Use one of: {string.Join(", ", CosmeticRules.Statuses)}."]
            });
        }

        if (request.Sort is not null && !CosmeticRules.Sorts.Contains(request.Sort))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Sort)] = [$"Unknown sort. Use one of: {string.Join(", ", CosmeticRules.Sorts)}."]
            });
        }

        var skip = request.Skip ?? 0;
        var take = request.Take ?? CosmeticRules.DefaultPageSize;

        // Server-local day, same rule as the reminder worker and the client helper.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var horizon = today.AddDays(CosmeticRules.ExpirySoonDays);

        var query = db.Items.AsNoTracking();

        // Closed location set: exact match, case-insensitive like Name.
        if (!string.IsNullOrWhiteSpace(request.Category) && request.Category is not CosmeticRules.CategoryAll)
            query = query.Where(x => EF.Functions.ILike(x.Category, EscapeLike(request.Category), @"\"));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{EscapeLike(request.Name)}%", @"\"));

        query = status switch
        {
            CosmeticRules.StatusExpired => query.Where(x => x.ExpiryDate < today),
            CosmeticRules.StatusSoon => query.Where(x => x.ExpiryDate >= today && x.ExpiryDate <= horizon),
            CosmeticRules.StatusGood => query.Where(x => x.ExpiryDate > horizon),
            _ => query
        };

        query = request.Sort switch
        {
            CosmeticRules.SortExpiryDesc => query.OrderByDescending(x => x.ExpiryDate).ThenBy(x => x.Id),
            CosmeticRules.SortName => query.OrderBy(x => x.Name).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.ExpiryDate).ThenBy(x => x.Id)
        };

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .Skip(skip)
            .Take(take)
            .Select(x => new CosmeticItemDto(
                x.Id,
                x.Name,
                x.Quantity,
                x.Unit,
                x.Category,
                x.Notes,
                x.ExpiryDate,
                x.HouseholdMemberId,
                x.Barcode))
            .ToListAsync(ct);

        return TypedResults.Ok(new GetCosmeticItemsResponse(
            items,
            totalItems));
    }

    [MapPost("/")]
    public async Task<Results<Created<CosmeticItemDto>, ValidationProblem>> CreateCosmeticItemAsync(
        CreateCosmeticItemRequest request,
        [FromHeader(Name = "X-Member-Id")] string? memberId,
        CancellationToken ct)
    {
        var entry = new CosmeticItem
        {
            Name = request.Name,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Category = request.Category,
            Notes = request.Notes,
            ExpiryDate = request.ExpiryDate,
            Barcode = request.Barcode,
            HouseholdMemberId = await ResolveMemberIdAsync(memberId, ct)
        };

        db.Items.Add(entry);
        await db.SaveChangesAsync(ct);
        await BroadcastAsync(new CosmeticChangedEvent(entry.Id, CosmeticChangeKind.Created), ct);

        return TypedResults.Created($"/api/cosmetics/{entry.Id}", ToDto(entry));
    }

    [MapGet("/{id}")]
    public async Task<Results<Ok<CosmeticItemDto>, ProblemHttpResult>> GetCosmeticItemAsync(
        string id,
        CancellationToken ct)
    {
        var item = await db.Items.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return item is null
            ? ItemNotFound(id)
            : TypedResults.Ok(ToDto(item));
    }

    [MapPut("/{id}")]
    public async Task<Results<Ok<CosmeticItemDto>, ProblemHttpResult, ValidationProblem>> UpdateCosmeticItemAsync(
        string id,
        UpdateCosmeticItemRequest request,
        CancellationToken ct)
    {
        var entry = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null)
            return ItemNotFound(id);

        // Attribution records who added the item and never changes afterwards.
        entry.Name = request.Name;
        entry.Quantity = request.Quantity;
        entry.Unit = request.Unit;
        entry.Category = request.Category;
        entry.Notes = request.Notes;
        entry.ExpiryDate = request.ExpiryDate;
        entry.Barcode = request.Barcode;
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await BroadcastAsync(new CosmeticChangedEvent(entry.Id, CosmeticChangeKind.Updated), ct);

        return TypedResults.Ok(ToDto(entry));
    }

    [MapDelete("/{id}")]
    public async Task<Results<NoContent, ProblemHttpResult>> DeleteCosmeticItemAsync(
        string id,
        CancellationToken ct)
    {
        var deleted = await db.Items.Where(x => x.Id == id).ExecuteDeleteAsync(ct);

        if (deleted == 0)
            return ItemNotFound(id);

        await BroadcastAsync(new CosmeticChangedEvent(id, CosmeticChangeKind.Removed), ct);
        return TypedResults.NoContent();
    }

    private static ProblemHttpResult ItemNotFound(string id)
        => EndpointProblems.NotFound("Cosmetic item", id);

    /// <summary>Escapes ILIKE wildcards so user input matches literally.</summary>
    private static string EscapeLike(string value) => value
        .Replace(@"\", @"\\")
        .Replace("%", @"\%")
        .Replace("_", @"\_");

    /// <summary>
    /// Realtime is a hint, never the write: a dead hub connection must not
    /// turn a committed change into a 500.
    /// </summary>
    private async Task BroadcastAsync(CosmeticChangedEvent change, CancellationToken ct)
    {
        try
        {
            await hub.Clients.All.CosmeticChanged(change, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Cosmetics realtime broadcast for {ItemId} failed.", change.ItemId);
        }
    }

    /// <summary>
    /// Attribution is a label, never access control: unknown or missing
    /// members resolve to null instead of failing the write.
    /// </summary>
    private async Task<string?> ResolveMemberIdAsync(string? memberId, CancellationToken ct)
        => string.IsNullOrWhiteSpace(memberId)
            ? null
            : await db.Members
                .AsNoTracking()
                .Where(x => x.Id == memberId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

    private static CosmeticItemDto ToDto(CosmeticItem item) => new(
        item.Id,
        item.Name,
        item.Quantity,
        item.Unit,
        item.Category,
        item.Notes,
        item.ExpiryDate,
        item.HouseholdMemberId,
        item.Barcode);
}