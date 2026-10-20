using Refit;

using Shared.Models;

namespace Client.Services;

[PathPrefix("/api/cosmetics")]
internal interface ICosmeticEndpoints
{
    [Get("/")]
    Task<ApiResponse<GetCosmeticItemsResponse>> GetAsync(
        GetCosmeticItemsRequest? request = null,
        CancellationToken ct = default);

    [Get("/{id}")]
    Task<ApiResponse<CosmeticItemDto>> GetByIdAsync(
        string id,
        CancellationToken ct = default);

    [Post("/")]
    Task<ApiResponse<CosmeticItemDto>> CreateAsync(
        [Body] CreateCosmeticItemRequest request,
        [Header("X-Member-Id")] string? memberId,
        CancellationToken ct = default);

    [Put("/{id}")]
    Task<ApiResponse<CosmeticItemDto>> UpdateAsync(
        string id,
        [Body] UpdateCosmeticItemRequest request,
        CancellationToken ct = default);

    [Delete("/{id}")]
    Task<IApiResponse> DeleteAsync(
        string id,
        CancellationToken ct = default);
}