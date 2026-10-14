using Refit;

using Shared.Models;

namespace Client.Services;

[PathPrefix("/api/pantry")]
internal interface IPantryEndpoints
{
    [Get("/")]
    Task<ApiResponse<GetPantryItemsResponse>> GetAsync(
        GetPantryItemsRequest? request = null,
        CancellationToken ct = default);

    [Get("/{id}")]
    Task<ApiResponse<PantryItemDto>> GetByIdAsync(
        string id,
        CancellationToken ct = default);

    [Post("/")]
    Task<ApiResponse<PantryItemDto>> CreateAsync(
        [Body] CreatePantryItemRequest request,
        [Header("X-Member-Id")] string? memberId,
        CancellationToken ct = default);

    [Put("/{id}")]
    Task<ApiResponse<PantryItemDto>> UpdateAsync(
        string id,
        [Body] UpdatePantryItemRequest request,
        CancellationToken ct = default);

    [Delete("/{id}")]
    Task<IApiResponse> DeleteAsync(
        string id,
        CancellationToken ct = default);
}