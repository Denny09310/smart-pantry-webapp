using Refit;

using Shared.Models;

namespace Client.Services.Api;

[Headers("Accept: application/json")]
public interface IProductEndpoints
{
    [Get("/api/products/lookup")]
    Task<ApiResponse<ProductLookupDto>> LookupAsync(
        [Query] string barcode,
        CancellationToken ct = default);
}