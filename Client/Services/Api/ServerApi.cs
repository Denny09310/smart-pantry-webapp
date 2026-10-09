using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;
using Shared.Models;

namespace Client.Services;

internal sealed class ServerApi(IServiceProvider services)
{
    public IPantryEndpoints Pantry => services.GetRequiredService<IPantryEndpoints>();
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

        services.AddScoped<ServerApi>();

        return services;
    }
}