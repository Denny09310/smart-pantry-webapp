using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Data.Entities;
using Server.Services;

using Shared.Models;

namespace Server.Endpoints;

[Tags("Products")]
[MapGroup("/api/products")]
internal class ProductEndpoints(ApplicationDbContext db, IProductLookupService lookup)
{
    [MapGet("/lookup")]
    public async Task<Results<Ok<ProductLookupDto>, NotFound, ValidationProblem>> LookupAsync(
        [AsParameters] LookupProductRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Barcode)] = ["Barcode must be 8, 12, or 13 digits."]
            });
        }

        // Local cache first: repeat scans never touch the network.
        var cached = await db.CachedProducts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Barcode == request.Barcode, ct);

        if (cached is not null)
            return TypedResults.Ok(ToDto(cached));

        // Miss: ask OpenBeautyFacts, then remember the answer.
        var product = await lookup.LookupAsync(request.Barcode, ct);

        if (product is not null)
        {
            await UpsertCachedProductAsync(product, ct);
            return TypedResults.Ok(product);
        }

        // Fall back to the local database for barcodes we know from
        // manually entered cosmetics that were never fetched remotely.
        var localProduct = await db.Items.AsNoTracking()
            .Where(x => x.Barcode == request.Barcode)
            .Select(x => new ProductLookupDto(
                x.Barcode!,
                x.Name,
                null,
                x.Quantity > 0 ? x.Quantity : null,
                x.Unit,
                null))
            .FirstOrDefaultAsync(ct);

        return localProduct is not null
            ? TypedResults.Ok(localProduct)
            : TypedResults.NotFound();
    }

    private async Task UpsertCachedProductAsync(ProductLookupDto product, CancellationToken ct)
    {
        var existing = await db.CachedProducts.FindAsync([product.Barcode], ct);

        if (existing is null)
        {
            db.CachedProducts.Add(FromDto(product));
        }
        else
        {
            existing.Name = product.Name;
            existing.Brand = product.Brand;
            existing.Quantity = product.Quantity;
            existing.Unit = product.Unit;
            existing.ImageUrl = product.ImageUrl;
            existing.IngredientsText = product.IngredientsText;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Best-effort cache: a concurrent lookup for the same barcode
            // can win the insert race, and that outcome is fine — the
            // product is cached either way, so just serve the result.
            db.ChangeTracker.Clear();
        }
    }

    private static ProductLookupDto ToDto(CachedProduct cached) => new(
        cached.Barcode,
        cached.Name,
        cached.Brand,
        cached.Quantity,
        cached.Unit,
        cached.ImageUrl,
        cached.IngredientsText);

    private static CachedProduct FromDto(ProductLookupDto product) => new()
    {
        Barcode = product.Barcode,
        Name = product.Name,
        Brand = product.Brand,
        Quantity = product.Quantity,
        Unit = product.Unit,
        ImageUrl = product.ImageUrl,
        IngredientsText = product.IngredientsText,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}