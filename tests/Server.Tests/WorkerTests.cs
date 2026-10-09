using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Server.Tests;

/// <summary>
/// The background expiration worker must not run inside the test host,
/// where it would race the tests against the shared database.
/// </summary>
[Collection("api")]
public sealed class WorkerTests(PantryApiFactory factory)
{
    [Fact]
    public void TestHost_Has_No_Expiration_Worker()
    {
        Assert.DoesNotContain(
            factory.Services.GetServices<IHostedService>(),
            s => s.GetType().Name.Contains("Expiration", StringComparison.Ordinal));
    }
}
