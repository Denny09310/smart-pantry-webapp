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
        var skip = request.Skip ?? 0;
        var take = request.Take ?? 20;

        var query = db.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Location) && request.Location is not "all")
            query = query.Where(x => x.Location.Contains(request.Location));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{request.Name}%"));

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
                x.HouseholdMemberId))
            .ToListAsync(ct);

        var totalItems = await query.CountAsync(ct);

        return TypedResults.Ok(new GetPantryItemsResponse(
            items,
            totalItems));
    }

    [MapPost("/")]
    public async Task<Results<Ok<PantryItemDto>, ValidationProblem>> CreatePantryItemAsync(
        CreatePantryItemRequest request,
        CancellationToken ct)
    {
        if (request.HouseholdMemberId is not null &&
            !await db.Members.AnyAsync(x => x.Id == request.HouseholdMemberId, ct))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.HouseholdMemberId)] = ["Unknown household member."]
            });
        }

        var entry = new PantryItem
        {
            Name = request.Name,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Location = request.Location,
            Notes = request.Notes,
            ExpirationDate = request.ExpirationDate,
            HouseholdMemberId = request.HouseholdMemberId
        };

        db.Items.Add(entry);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new PantryItemDto(
            entry.Id,
            entry.Name,
            entry.Quantity,
            entry.Unit,
            entry.Location,
            entry.Notes,
            entry.ExpirationDate,
            entry.HouseholdMemberId));
    }
}
