using System.Text.Json;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Server.Data;

namespace Server.Services;

/// <summary>Readiness probe: Postgres reachable.</summary>
public sealed class DatabaseHealthCheck(IServiceScopeFactory scopes) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy("Postgres reachable.")
                : HealthCheckResult.Unhealthy("Postgres unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres check failed.", ex);
        }
    }
}

/// <summary>JSON health responses without exception details.</summary>
public static class HealthResponses
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task WriteJsonAsync(HttpContext http, HealthReport report)
    {
        http.Response.ContentType = "application/json";

        return http.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        }, Json);
    }
}
