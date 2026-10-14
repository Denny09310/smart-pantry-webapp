using Refit;

using Shared.Models;

namespace Client.Services;

[PathPrefix("/api/members")]
internal interface IMemberEndpoints
{
    [Get("/")]
    Task<ApiResponse<List<HouseholdMemberDto>>> GetAsync(
        CancellationToken ct = default);

    [Post("/")]
    Task<ApiResponse<HouseholdMemberDto>> CreateAsync(
        [Body] CreateHouseholdMemberRequest request,
        CancellationToken ct = default);

    [Put("/{id}")]
    Task<ApiResponse<HouseholdMemberDto>> UpdateAsync(
        string id,
        [Body] UpdateHouseholdMemberRequest request,
        CancellationToken ct = default);
}