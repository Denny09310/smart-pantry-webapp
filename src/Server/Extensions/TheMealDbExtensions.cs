namespace Server.Extensions;

/// <summary>
/// Outbound TheMealDB client (free key, single-ingredient filters).
/// </summary>
public static class TheMealDbExtensions
{
    /// <summary>
    /// Registers the named TheMealDB client: base address and a tolerant
    /// timeout. Empty results and outages surface as empty suggestion
    /// lists, never errors.
    /// </summary>
    public static IServiceCollection AddTheMealDb(this IServiceCollection services)
    {
        services.AddHttpClient("TheMealDb", client =>
        {
            client.BaseAddress = new Uri("https://www.themealdb.com/api/json/v1/1/");
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "SmartPantry/0.2 (https://github.com/Denny09310/smart-pantry-webapp)");
        });

        return services;
    }
}
