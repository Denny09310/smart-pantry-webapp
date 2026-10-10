using System.Security.Cryptography;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;

namespace Server.Tests;

/// <summary>
/// Boots the real API (entry assembly located via <see cref="ApplicationDbContext"/>)
/// against an isolated Postgres database (<c>smart_pantry_tests</c>). The app's startup migration creates it on
/// first boot. All test classes share one collection (and factory), so they
/// run sequentially against the same database.
/// Upstream Open Food Facts calls are stubbed: resolve <see cref="StubOffHandler"/>
/// from <see cref="WebApplicationFactory{TEntryPoint}.Services"/> to program responses.
/// </summary>
public sealed class PantryApiFactory : WebApplicationFactory<ApplicationDbContext>
{
    private const string TestConnectionString =
        "Server=localhost;Database=smart_pantry_tests;Uid=postgres;Pwd=mypassword123";

    public PantryApiFactory()
    {
        // Program.Main reads the connection string eagerly, before the test
        // host applies its own configuration, so it must already be visible
        // through the environment when the host builds.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", TestConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var contentRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Server"));
        builder.UseContentRoot(contentRoot);
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestConnectionString,
                ["Vapid:PublicKey"] = TestVapidKeys.PublicKey,
                ["Vapid:PrivateKey"] = TestVapidKeys.PrivateKey,
            }));
        builder.ConfigureTestServices(services =>
        {
            var stub = new StubOffHandler();
            services.AddSingleton(stub);
            services.AddSingleton<IHttpClientFactory>(_ => new StubHttpClientFactory(stub));
        });
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", null);
        base.Dispose(disposing);
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<PantryApiFactory>;