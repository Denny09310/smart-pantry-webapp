using Refit;

using Shared.Models;

namespace Client.Services;

[PathPrefix("/api/push")]
internal interface IPushEndpoints
{
    [Get("/public-key")]
    Task<ApiResponse<GetPushNotificationKey>> GetPushNotificationKeyAsync(
        CancellationToken ct = default);

    [Post("/subscriptions")]
    Task<ApiResponse<PushSubscriptionDto>> SubscribeAsync(
        [Body] PushSubscriptionRequest request,
        CancellationToken ct = default);

    [Delete("/subscriptions")]
    Task<IApiResponse> UnsubscribeAsync(
        [Query] string endpoint,
        CancellationToken ct = default);
}