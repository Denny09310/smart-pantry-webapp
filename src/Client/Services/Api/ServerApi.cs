using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Refit;

using Shared.Models;

namespace Client.Services;

internal sealed class ServerApi(IServiceProvider services)
{
    public IPantryEndpoints Pantry => services.GetRequiredService<IPantryEndpoints>();

    public INotificationEndpoints Notifications => services.GetRequiredService<INotificationEndpoints>();

    public IPushEndpoints Push => services.GetRequiredService<IPushEndpoints>();
}

[PathPrefix("/api/pantry")]
internal interface IPantryEndpoints
{
    [Get("/")]
    Task<ApiResponse<GetPantryItemsResponse>> GetAsync(
        GetPantryItemsRequest? request = null,
        CancellationToken ct = default);

    [Post("/")]
    Task<ApiResponse<PantryItemDto>> CreateAsync(
        CreatePantryItemRequest request,
        CancellationToken ct = default);

    [Delete("/{id}")]
    Task<IApiResponse> DeleteAsync(
        string id,
        CancellationToken ct = default);

    [Put("/{id}")]
    Task<ApiResponse<PantryItemDto>> UpdateAsync(
        string id,
        UpdatePantryItemRequest request,
        CancellationToken ct = default);
}

[PathPrefix("/api/notifications")]
internal interface INotificationEndpoints
{
    [Get("/")]
    Task<ApiResponse<GetNotificationsResponse>> GetAsync(
        CancellationToken ct = default);

    [Post("/{id}/read")]
    Task<ApiResponse<NotificationDto>> MarkReadAsync(
        string id,
        CancellationToken ct = default);

    [Post("/read-all")]
    Task<ApiResponse<int>> MarkAllReadAsync(
        CancellationToken ct = default);
}

[PathPrefix("/api/push")]
internal interface IPushEndpoints
{
    [Get("/vapid-public-key")]
    Task<ApiResponse<PushPublicKeyResponse>> GetPublicKeyAsync(
        CancellationToken ct = default);

    [Post("/subscriptions")]
    Task<ApiResponse<PushSubscriptionDto>> SubscribeAsync(
        PushSubscriptionRequest request,
        CancellationToken ct = default);

    [Delete("/subscriptions")]
    Task<IApiResponse> UnsubscribeAsync(
        [Query] string endpoint,
        CancellationToken ct = default);
}

internal static class ServerApiExtensions
{
    public static IServiceCollection AddServerApi(this IServiceCollection services)
    {
        services.AddHttpClient("Server.API", (sp, client) =>
        {
            var env = sp.GetRequiredService<IWebAssemblyHostEnvironment>();
            client.BaseAddress = new(env.BaseAddress);
        });

        services.AddRefitGeneratedClient<IPantryEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddRefitGeneratedClient<INotificationEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddRefitGeneratedClient<IPushEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddScoped<ServerApi>();

        return services;
    }
}