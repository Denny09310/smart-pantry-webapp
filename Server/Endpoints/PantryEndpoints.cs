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
            query = query.Where(x => x.Location.Contains(request.Location));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{request.Name}%"));

        var items = await query
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(x => new PantryItemDto(
                x.Id,
                x.Name,
                x.Quantity,
                x.Unit,
                x.Location,
                x.Notes,
                x.ExpirationDate))
            .ToListAsync(ct);

        var totalItems = await query.CountAsync(ct);

        return TypedResults.Ok(new GetPantryItemsResponse(
            items,
            totalItems));
    }

    [MapPost("/")]
    public async Task<Ok<PantryItemDto>> CreatePantryItemAsync(
        CreatePantryItemRequest request,
        CancellationToken ct)
    {
        var entry = new PantryItem
        {
            Name = request.Name,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Location = request.Location,
            Notes = request.Notes,
            ExpirationDate = request.ExpirationDate
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
            entry.ExpirationDate));
    }
}
