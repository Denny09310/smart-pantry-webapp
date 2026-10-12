using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

namespace Client.Services;

internal sealed class ServerApi(IServiceProvider services)
{
    public IPantryEndpoints Pantry => services.GetRequiredService<IPantryEndpoints>();
    public IMemberEndpoints Members => services.GetRequiredService<IMemberEndpoints>();
    public INotificationEndpoints Notifications => services.GetRequiredService<INotificationEndpoints>();
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

        services.AddRefitGeneratedClient<IMemberEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddRefitGeneratedClient<INotificationEndpoints>(
            settings: null,
            httpClientName: "Server.API");

        services.AddScoped<ServerApi>();

        return services;
    }
}
