namespace Server.Extensions;

/// <summary>
/// Outbound Open Food Facts client.
/// </summary>
public static class OpenFoodFactsExtensions
{
    /// <summary>
    /// Registers the named Open Food Facts client: base address, tolerant
    /// timeout, and the descriptive User-Agent the API requires.
    /// </summary>
    public static IServiceCollection AddOpenFoodFacts(this IServiceCollection services)
    {
        services.AddHttpClient("OpenFoodFacts", client =>
        {
            client.BaseAddress = new Uri("https://world.openfoodfacts.org/");
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "SmartPantry/0.2 (https://github.com/Denny09310/smart-pantry-webapp)");
        });

        return services;
    }
}
