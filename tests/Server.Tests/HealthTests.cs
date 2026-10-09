using System.Net;

namespace Server.Tests;

[Collection("api")]
public sealed class HealthTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_Reports_App_And_Database_Status()
    {
        using var response = await _client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
