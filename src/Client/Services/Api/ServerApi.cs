using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Refit;

using Shared.Models;

namespace Client.Services;

internal sealed class ServerApi(IServiceProvider services)
{
    public IPantryEndpoints Pantry => services.GetRequiredService<IPantryEndpoints>();

    public INotificationEndpoints Notifications => services.GetRequiredService<INotificationEndpoints>();

    public IPushEndpoints Push => services.GetRequiredService<IPushEndpoints>();

    public IMemberEndpoints Members => services.GetRequiredService<IMemberEndpoints>();

    /// <summary>
    /// Loads the whole pantry, paging with Skip/Take until TotalItems is
    /// reached. A single request is capped server-side, so one Take is not
    /// enough once the pantry outgrows the page.
    /// </summary>
    public async Task<List<PantryItemDto>?> GetAllPantryItemsAsync(CancellationToken ct = default)
    {
        const int pageSize = 100;

        List<PantryItemDto> all = [];
        var total = int.MaxValue;

        while (all.Count < total)
        {
            using var response = await Pantry.GetAsync(
                new GetPantryItemsRequest(null, null) { Skip = all.Count, Take = pageSize },
                ct);

            if (!response.IsSuccessfulWithContent || response.Content is null)
                return null;

            var page = response.Content.Items.ToList();

            if (page.Count == 0)
                break;

            all.AddRange(page);
            total = response.Content.TotalItems;
        }

        return all;
    }

    public IProductEndpoints Products => services.GetRequiredService<IProductEndpoints>();

    public IRecipeEndpoints Recipes => services.GetRequiredService<IRecipeEndpoints>();
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
        [Header("X-Member-Id")] string? memberId,
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

[PathPrefix("/api/members")]
internal interface IMemberEndpoints
{
    [Get("/")]
    Task<ApiResponse<List<MemberDto>>> GetAsync(
        CancellationToken ct = default);

    [Post("/")]
    Task<ApiResponse<MemberDto>> CreateAsync(
        CreateMemberRequest request,
        CancellationToken ct = default);

    [Delete("/{id}")]
    Task<IApiResponse> DeleteAsync(
        string id,
        CancellationToken ct = default);
}

[PathPrefix("/api/products")]
internal interface IProductEndpoints
{
    [Get("/lookup")]
    Task<ApiResponse<ProductLookupDto>> LookupAsync(
        [Query] string barcode,
        CancellationToken ct = default);
}

[PathPrefix("/api/recipes")]
internal interface IRecipeEndpoints
{
    [Get("/suggestions")]
    Task<ApiResponse<GetRecipeSuggestionsResponse>> GetSuggestionsAsync(
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

        services.AddRefitGeneratedClient<IMemberEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddRefitGeneratedClient<IProductEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddRefitGeneratedClient<IRecipeEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddScoped<ServerApi>();

        return services;
    }
}