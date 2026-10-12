using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Data;
using Shared.Models;

namespace Server.Tests;

public class PantryApiTests(PantryApiFactory factory) : IClassFixture<PantryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly PantryApiFactory _factory = factory;

    [Fact]
    public async Task Get_ReturnsOk_WithExpectedShape()
    {
        var response = await _client.GetAsync("/api/pantry");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetPantryItemsResponse>();
        Assert.NotNull(body);
        Assert.True(body.TotalItems >= 0);
        Assert.NotNull(body.Items);
    }

    [Fact]
    public async Task Get_TakeAboveMaximum_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/pantry?take=200");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("Take", out _));
    }

    [Fact]
    public async Task Post_InvalidRequest_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/pantry", new CreatePantryItemRequest(
            "", -1, "L", "fridge", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out _));
        Assert.True(errors.TryGetProperty("Quantity", out _));
    }

    [Fact]
    public async Task Post_UnknownMember_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/pantry", new CreatePantryItemRequest(
            "Milk", 1, "L", "fridge", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            HouseholdMemberId: "does-not-exist"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("HouseholdMemberId", out _));
    }

    [Fact]
    public async Task Post_ValidRequest_CreatesItem_VisibleInList()
    {
        var name = $"test-milk-{Guid.CreateVersion7()}";

        var create = await _client.PostAsJsonAsync("/api/pantry", new CreatePantryItemRequest(
            name, 1, "L", "fridge", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));

        try
        {
            Assert.Equal(HttpStatusCode.OK, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<PantryItemDto>();
            Assert.NotNull(created);
            Assert.False(string.IsNullOrWhiteSpace(created.Id));
            Assert.Equal(name, created.Name);

            var list = await _client.GetFromJsonAsync<GetPantryItemsResponse>(
                $"/api/pantry?name={Uri.EscapeDataString(name)}");
            Assert.NotNull(list);
            Assert.Contains(list.Items, x => x.Id == created.Id);
        }
        finally
        {
            await DeleteItemsByNameAsync(name);
        }
    }

    private async Task DeleteItemsByNameAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Items.Where(x => x.Name == name).ExecuteDeleteAsync();
    }
}
