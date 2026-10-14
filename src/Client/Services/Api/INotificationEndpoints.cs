using Refit;

using Shared.Models;

namespace Client.Services;

[PathPrefix("/api/notifications")]
internal interface INotificationEndpoints
{
    [Get("/")]
    Task<ApiResponse<GetNotificationsResponse>> GetAsync(
        GetNotificationsRequest? request = null,
        CancellationToken ct = default);

    [Post("/{id}/read")]
    Task<ApiResponse<NotificationDto>> MarkReadAsync(
        string id,
        CancellationToken ct = default);

    [Post("/read-all")]
    Task<ApiResponse<int>> MarkAllReadAsync(
        CancellationToken ct = default);
}