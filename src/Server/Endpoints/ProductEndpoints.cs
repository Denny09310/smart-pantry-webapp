using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;

using Server.Services;

using Shared.Models;

namespace Server.Endpoints;

[Tags("Products")]
[MapGroup("/api/products")]
internal class ProductEndpoints(ProductLookupService lookup)
{
    [MapGet("/lookup")]
    public async Task<Results<Ok<ProductLookupDto>, NotFound, ValidationProblem>> LookupAsync(
        [AsParameters] LookupProductRequest request,
        CancellationToken ct)
    {
        var product = await lookup.LookupAsync(request.Barcode!, ct);

        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(product);
    }
}
