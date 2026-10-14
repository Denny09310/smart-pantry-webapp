using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Server.Tests;

public class HealthTests(PantryApiFactory factory) : IClassFixture<PantryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Live_Returns_Healthy()
    {
        using var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_Returns_Healthy_With_Database()
    {
        using var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
    }
}