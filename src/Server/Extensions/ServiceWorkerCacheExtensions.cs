namespace Server.Extensions;

/// <summary>
/// Response headers for service worker delivery.
/// </summary>
public static class ServiceWorkerCacheExtensions
{
    /// <summary>
    /// Prevents HTTP caching of the service worker scripts and the Bswup engine.
    /// A cached worker pins clients to an old version.
    /// </summary>
    public static IApplicationBuilder UseServiceWorkerNoCache(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Set on the way out: static-assets middleware sets its own
            // Cache-Control and would otherwise win.
            context.Response.OnStarting(() =>
            {
                var path = context.Request.Path.Value;
                if (path is "/service-worker.js" or "/service-worker.published.js"
                    || (path is not null && path.StartsWith("/_content/Bit.Bswup/", StringComparison.Ordinal)))
                {
                    context.Response.Headers.CacheControl = "no-cache";
                }

                return Task.CompletedTask;
            });

            await next(context);
        });

        return app;
    }
}