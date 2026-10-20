using Client.Services.Api;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Refit;

namespace Client.Services;

internal sealed class ServerApi(IServiceProvider services)
{
    public ICosmeticEndpoints Cosmetics => services.GetRequiredService<ICosmeticEndpoints>();
    public IMemberEndpoints Members => services.GetRequiredService<IMemberEndpoints>();
    public INotificationEndpoints Notifications => services.GetRequiredService<INotificationEndpoints>();
    public IPushEndpoints Push => services.GetRequiredService<IPushEndpoints>();
    public IProductEndpoints Products => services.GetRequiredService<IProductEndpoints>();
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

        services.AddRefitGeneratedClient<ICosmeticEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddRefitGeneratedClient<IMemberEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddRefitGeneratedClient<INotificationEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddRefitGeneratedClient<IPushEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddRefitGeneratedClient<IProductEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddScoped<ServerApi>();

        return services;
    }
}