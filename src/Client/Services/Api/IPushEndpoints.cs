using Refit;
using Shared.Models;

namespace Client.Services;

[PathPrefix("/api/push")]
internal interface IPushEndpoints
{
    [Get("/public-key")]
    Task<ApiResponse<GetPushNotificationKey>> GetPushNotificationKeyAsync(
        CancellationToken ct = default);
}