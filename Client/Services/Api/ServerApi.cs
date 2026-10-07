using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

namespace Client.Services;

internal sealed class ServerApi(IServiceProvider services)
{
    public IGreetingsApi Greetings => services.GetRequiredService<IGreetingsApi>();
}

[PathPrefix("/api/greetings")]
internal interface IGreetingsApi
{
    [Get("/")]
    Task<ApiResponse<string>> GetAsync();
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

        services.AddRefitGeneratedClient<IGreetingsApi>(
            settings: null,
            httpClientName: "Server.API");

        services.AddScoped<ServerApi>();

        return services;
    }
}