namespace Server.Extensions;

/// <summary>
/// Outbound MyMemory client (free keyless translation, it|en).
/// </summary>
public static class MyMemoryExtensions
{
    /// <summary>
    /// Registers the named MyMemory client: base address and a short
    /// timeout. Translation is best-effort; callers fall back to the
    /// original name on any failure.
    /// </summary>
    public static IServiceCollection AddMyMemory(this IServiceCollection services)
    {
        services.AddHttpClient("MyMemory", client =>
        {
            client.BaseAddress = new Uri("https://api.mymemory.translated.net/");
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "SmartPantry/0.2 (https://github.com/Denny09310/smart-pantry-webapp)");
        });

        return services;
    }
}
