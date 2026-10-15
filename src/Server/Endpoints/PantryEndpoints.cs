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
internal class PantryEndpoints(
    ApplicationDbContext db,
    IHubContext<PantryHub, IPantryHubClient> hub,
    ILogger<PantryEndpoints> log)
{
    [MapGet("/")]
    public async Task<Results<Ok<GetPantryItemsResponse>, ValidationProblem>> GetPantryItemsAsync(
        [AsParameters] GetPantryItemsRequest request,
        CancellationToken ct)
    {
        // "all" is documented as unfiltered: normalize before validating.
        var status = request.Status is PantryRules.StatusAll ? null : request.Status;

        if (status is not null && !PantryRules.Statuses.Contains(status))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Status)] = [$"Unknown status. Use one of: {string.Join(", ", PantryRules.Statuses)}."]
            });
        }

        if (request.Sort is not null && !PantryRules.Sorts.Contains(request.Sort))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Sort)] = [$"Unknown sort. Use one of: {string.Join(", ", PantryRules.Sorts)}."]
            });
        }

        var skip = request.Skip ?? 0;
        var take = request.Take ?? PantryRules.DefaultPageSize;

        // Server-local day, same rule as the reminder worker and the client helper.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var horizon = today.AddDays(PantryRules.ExpirySoonDays);

        var query = db.Items.AsNoTracking();

        // Closed location set: exact match, case-insensitive like Name.
        if (!string.IsNullOrWhiteSpace(request.Location) && request.Location is not PantryRules.LocationAll)
            query = query.Where(x => EF.Functions.ILike(x.Location, EscapeLike(request.Location), @"\"));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{EscapeLike(request.Name)}%", @"\"));

        query = status switch
        {
            PantryRules.StatusExpired => query.Where(x => x.ExpirationDate < today),
            PantryRules.StatusSoon => query.Where(x => x.ExpirationDate >= today && x.ExpirationDate <= horizon),
            PantryRules.StatusFresh => query.Where(x => x.ExpirationDate > horizon),
            _ => query
        };

        query = request.Sort switch
        {
            PantryRules.SortExpiryDesc => query.OrderByDescending(x => x.ExpirationDate).ThenBy(x => x.Id),
            PantryRules.SortName => query.OrderBy(x => x.Name).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.ExpirationDate).ThenBy(x => x.Id)
        };

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .Skip(skip)
            .Take(take)
            .Select(x => new PantryItemDto(
                x.Id,
                x.Name,
                x.Quantity,
                x.Unit,
                x.Location,
                x.Notes,
                x.ExpirationDate,
                x.HouseholdMemberId,
                x.Barcode,
                x.NutriScore,
                NutritionFactsExtensions.FromJson(x.NutritionJson)))
            .ToListAsync(ct);

        return TypedResults.Ok(new GetPantryItemsResponse(
            items,
            totalItems));
    }

    [MapPost("/")]
    public async Task<Results<Created<PantryItemDto>, ValidationProblem>> CreatePantryItemAsync(
        CreatePantryItemRequest request,
        [FromHeader(Name = "X-Member-Id")] string? memberId,
        CancellationToken ct)
    {
        var entry = new PantryItem
        {
            Name = request.Name,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Location = request.Location,
            Notes = request.Notes,
            ExpirationDate = request.ExpirationDate,
            Barcode = request.Barcode,
            NutriScore = request.NutriScore,
            NutritionJson = request.Nutrition is not null ? request.Nutrition.ToJson() : null,
            HouseholdMemberId = await ResolveMemberIdAsync(memberId, ct)
        };

        db.Items.Add(entry);
        await db.SaveChangesAsync(ct);
        await BroadcastAsync(new PantryChangedEvent(entry.Id, PantryChangeKind.Created), ct);

        return TypedResults.Created($"/api/pantry/{entry.Id}", ToDto(entry));
    }

    [MapGet("/{id}")]
    public async Task<Results<Ok<PantryItemDto>, ProblemHttpResult>> GetPantryItemAsync(
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
    public async Task<Results<Ok<PantryItemDto>, ProblemHttpResult, ValidationProblem>> UpdatePantryItemAsync(
        string id,
        UpdatePantryItemRequest request,
        CancellationToken ct)
    {
        var entry = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null)
            return ItemNotFound(id);

        // Attribution records who added the item and never changes afterwards.
        entry.Name = request.Name;
        entry.Quantity = request.Quantity;
        entry.Unit = request.Unit;
        entry.Location = request.Location;
        entry.Notes = request.Notes;
        entry.ExpirationDate = request.ExpirationDate;
        entry.Barcode = request.Barcode;
        entry.NutriScore = request.NutriScore;
        entry.NutritionJson = request.Nutrition?.ToJson();
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await BroadcastAsync(new PantryChangedEvent(entry.Id, PantryChangeKind.Updated), ct);

        return TypedResults.Ok(ToDto(entry));
    }

    [MapDelete("/{id}")]
    public async Task<Results<NoContent, ProblemHttpResult>> DeletePantryItemAsync(
        string id,
        CancellationToken ct)
    {
        var deleted = await db.Items.Where(x => x.Id == id).ExecuteDeleteAsync(ct);

        if (deleted == 0)
            return ItemNotFound(id);

        await BroadcastAsync(new PantryChangedEvent(id, PantryChangeKind.Removed), ct);
        return TypedResults.NoContent();
    }

    private static ProblemHttpResult ItemNotFound(string id)
        => EndpointProblems.NotFound("Pantry item", id);

    /// <summary>Escapes ILIKE wildcards so user input matches literally.</summary>
    private static string EscapeLike(string value) => value
        .Replace(@"\", @"\\")
        .Replace("%", @"\%")
        .Replace("_", @"\_");

    /// <summary>
    /// Realtime is a hint, never the write: a dead hub connection must not
    /// turn a committed change into a 500.
    /// </summary>
    private async Task BroadcastAsync(PantryChangedEvent change, CancellationToken ct)
    {
        try
        {
            await hub.Clients.All.PantryChanged(change, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Pantry realtime broadcast for {ItemId} failed.", change.ItemId);
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

    private static PantryItemDto ToDto(PantryItem item) => new(
        item.Id,
        item.Name,
        item.Quantity,
        item.Unit,
        item.Location,
        item.Notes,
        item.ExpirationDate,
        item.HouseholdMemberId,
        item.Barcode,
        item.NutriScore,
        NutritionFactsExtensions.FromJson(item.NutritionJson));
}