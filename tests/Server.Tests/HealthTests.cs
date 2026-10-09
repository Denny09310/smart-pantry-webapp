using System.Net;

namespace Server.Tests;

[Collection("api")]
public sealed class HealthTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Liveness_Reports_Healthy()
    {
        using var response = await _client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_Reports_Database_Status()
    {
        using var response = await _client.GetAsync("/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
