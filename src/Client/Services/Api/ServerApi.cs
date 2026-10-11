using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Refit;

using Client.Components.Shared;
using Client.Models;

using Shared.Models;

namespace Client.Services;

internal sealed class ServerApi(IServiceProvider services)
{
    public IPantryEndpoints Pantry => services.GetRequiredService<IPantryEndpoints>();

    public INotificationEndpoints Notifications => services.GetRequiredService<INotificationEndpoints>();

    public IPushEndpoints Push => services.GetRequiredService<IPushEndpoints>();

    public IMemberEndpoints Members => services.GetRequiredService<IMemberEndpoints>();

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

    // Offset paging: Skip is the token, TotalItems ends the sequence.
    [Get("/")]
    [Paged(Total = nameof(GetPantryItemsResponse.TotalItems))]
    PagedEnumerable<GetPantryItemsResponse, PantryItemDto> ListAsync(
        string? location,
        string? name,
        ExpiryStatus? status,
        bool excludeFresh,
        bool sortDescending,
        int take,
        [PageToken] int skip = 0);

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
    /// <summary>
    /// Reads one page (at most) and returns its items plus the total.
    /// </summary>
    public static async Task<InfiniteScrollList<PantryItemDto>.PageResult> GetPantryPageAsync(
        IPantryEndpoints pantry,
        string? location,
        string? name,
        ExpiryStatus? status,
        bool excludeFresh,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken ct)
    {
        await foreach (var page in pantry
            .ListAsync(location, name, status, excludeFresh, sortDescending, take, skip)
            .WithMaxPages(1)
            .AsPages()
            .WithCancellation(ct))
        {
            return new(page.Items.ToList(), page.TotalItems);
        }

        return new([], 0);
    }

    /// <summary>
    /// Chip counts, one Take=1 paged request per status plus one per storage
    /// location. All run in parallel.
    /// </summary>
    public static async Task<PantryCounts> GetPantryCountsAsync(
        IPantryEndpoints pantry,
        CancellationToken ct)
    {
        var total = CountAsync(pantry, null, null, false, ct);
        var expired = CountAsync(pantry, null, ExpiryStatus.Expired, false, ct);
        var today = CountAsync(pantry, null, ExpiryStatus.Today, false, ct);
        var soon = CountAsync(pantry, null, ExpiryStatus.Soon, false, ct);
        var fresh = CountAsync(pantry, null, ExpiryStatus.Fresh, false, ct);
        var attention = CountAsync(pantry, null, null, true, ct);

        var locations = new[]
        {
            ("pantry", CountAsync(pantry, "pantry", null, false, ct)),
            ("fridge", CountAsync(pantry, "fridge", null, false, ct)),
            ("freezer", CountAsync(pantry, "freezer", null, false, ct)),
            ("cabinet", CountAsync(pantry, "cabinet", null, false, ct)),
        };

        await Task.WhenAll(
            [total, expired, today, soon, fresh, attention,
             .. locations.Select(l => l.Item2)]);

        return new(
            await total,
            await expired,
            await today,
            await soon,
            await fresh,
            await attention,
            locations.ToDictionary(l => l.Item1, l => l.Item2.Result));
    }

    private static async Task<int> CountAsync(
        IPantryEndpoints pantry,
        string? location,
        ExpiryStatus? status,
        bool excludeFresh,
        CancellationToken ct)
    {
        await foreach (var page in pantry
            .ListAsync(location, null, status, excludeFresh, false, 1)
            .WithMaxPages(1)
            .AsPages()
            .WithCancellation(ct))
        {
            return page.TotalItems;
        }

        return 0;
    }
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