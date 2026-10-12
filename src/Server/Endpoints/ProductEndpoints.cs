using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Server.Data;
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
            return TypedResults.NotFound();

        // Try OpenFoodFacts first
        var product = await lookup.LookupAsync(request.Barcode, ct);

        if (product is not null)
            return TypedResults.Ok(product);

        // Fall back to local database for items we've already scanned
        var localProduct = await db.Items.AsNoTracking()
            .Where(x => x.Barcode == request.Barcode)
            .Select(x => new ProductLookupDto(
                x.Barcode!,
                x.Name,
                null,
                x.Quantity > 0 ? x.Quantity : null,
                x.Unit,
                x.NutriScore,
                x.NutritionJson != null ? NutritionFactsExtensions.Parse(x.NutritionJson) : null,
                null))
            .FirstOrDefaultAsync(ct);

        return localProduct is not null
            ? TypedResults.Ok(localProduct)
            : TypedResults.NotFound();
    }
}