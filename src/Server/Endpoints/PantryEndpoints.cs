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
        var items = (await query
            .OrderBy(x => x.ExpirationDate)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct))
            .Select(ToDto)
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
        var entry = await db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        return entry is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToDto(entry));
    }

    [MapPost("/")]
    public async Task<Results<Ok<PantryItemDto>, ValidationProblem>> CreatePantryItemAsync(
        CreatePantryItemRequest request,
        CancellationToken ct)
    {
        if (Validate(request) is { } errors)
            return TypedResults.ValidationProblem(errors);

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
        if (Validate(request) is { } errors)
            return TypedResults.ValidationProblem(errors);

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

    private static PantryItemDto ToDto(PantryItem entry)
        => new(
            entry.Id,
            entry.Name,
            entry.Quantity,
            entry.Unit,
            entry.Location,
            entry.Notes,
            entry.ExpirationDate,
            ExpiryStatusCalculator.GetStatus(entry.ExpirationDate, DateOnly.FromDateTime(DateTime.Today)));

    private static Dictionary<string, string[]>? Validate(
        string name,
        double quantity,
        string unit,
        string location,
        string? notes)
    {
        Dictionary<string, string[]>? errors = null;

        void Add(string field, string message)
        {
            (errors ??= []).Add(field, [message]);
        }

        if (string.IsNullOrWhiteSpace(name))
            Add(nameof(name), "Name is required.");
        else if (name.Trim().Length > 100)
            Add(nameof(name), "Name must be 100 characters or fewer.");

        if (quantity is < 1 or > 9999)
            Add(nameof(quantity), "Quantity must be between 1 and 9999.");

        if (string.IsNullOrWhiteSpace(unit))
            Add(nameof(unit), "Unit is required.");

        if (string.IsNullOrWhiteSpace(location))
            Add(nameof(location), "Location is required.");

        if (notes is { Length: > 500 })
            Add(nameof(notes), "Notes must be 500 characters or fewer.");

        return errors;
    }

    private static Dictionary<string, string[]>? Validate(CreatePantryItemRequest request)
        => Validate(request.Name, request.Quantity, request.Unit, request.Location, request.Notes);

    private static Dictionary<string, string[]>? Validate(UpdatePantryItemRequest request)
        => Validate(request.Name, request.Quantity, request.Unit, request.Location, request.Notes);
}
