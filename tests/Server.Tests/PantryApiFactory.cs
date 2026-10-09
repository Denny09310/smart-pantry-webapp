using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Server.Data;

namespace Server.Tests;

/// <summary>
/// Boots the real API (entry assembly located via <see cref="ApplicationDbContext"/>)
/// against an isolated Postgres database (<c>smart_pantry_tests</c>). The app's startup migration creates it on
/// first boot. All test classes share one collection (and factory), so they
/// run sequentially against the same database.
/// </summary>
public sealed class PantryApiFactory : WebApplicationFactory<ApplicationDbContext>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var contentRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Server"));
        builder.UseContentRoot(contentRoot);
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=localhost;Database=smart_pantry_tests;Uid=postgres;Pwd=mypassword123",
            }));
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<PantryApiFactory>;
