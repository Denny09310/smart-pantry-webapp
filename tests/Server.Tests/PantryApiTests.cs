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

    [Fact]
    public async Task Post_MalformedExpirationDate_ReturnsBadRequest()
    {
        using var content = new StringContent(
            """{"name":"Milk","quantity":1,"unit":"L","location":"fridge","expirationDate":"not-a-date"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await _client.PostAsync("/api/pantry", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFoundProblem()
    {
        var response = await _client.GetAsync($"/api/pantry/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetById_Existing_ReturnsItem()
    {
        var name = $"test-item-{Guid.CreateVersion7()}";
        var created = await CreateItemAsync(name);

        try
        {
            var response = await _client.GetAsync($"/api/pantry/{created.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var item = await response.Content.ReadFromJsonAsync<PantryItemDto>();
            Assert.NotNull(item);
            Assert.Equal(created.Id, item.Id);
            Assert.Equal(name, item.Name);
        }
        finally
        {
            await DeleteItemsByNameAsync(name);
        }
    }

    [Fact]
    public async Task Put_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/pantry/{Guid.CreateVersion7()}",
            new UpdatePantryItemRequest("Milk", 1, "L", "fridge", null,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_InvalidRequest_ReturnsValidationProblem()
    {
        var name = $"test-item-{Guid.CreateVersion7()}";
        var created = await CreateItemAsync(name);

        try
        {
            var response = await _client.PutAsJsonAsync($"/api/pantry/{created.Id}",
                new UpdatePantryItemRequest("", -1, "L", "fridge", null,
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await DeleteItemsByNameAsync(name);
        }
    }

    [Fact]
    public async Task Put_UnknownMember_ReturnsValidationProblem()
    {
        var name = $"test-item-{Guid.CreateVersion7()}";
        var created = await CreateItemAsync(name);

        try
        {
            var response = await _client.PutAsJsonAsync($"/api/pantry/{created.Id}",
                new UpdatePantryItemRequest("Milk", 1, "L", "fridge", null,
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    HouseholdMemberId: "does-not-exist"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await DeleteItemsByNameAsync(name);
        }
    }

    [Fact]
    public async Task Put_ValidRequest_UpdatesItem()
    {
        var name = $"test-item-{Guid.CreateVersion7()}";
        var created = await CreateItemAsync(name);

        try
        {
            var response = await _client.PutAsJsonAsync($"/api/pantry/{created.Id}",
                new UpdatePantryItemRequest("Oat milk", 2, "L", "pantry", "updated",
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<PantryItemDto>();
            Assert.NotNull(updated);
            Assert.Equal("Oat milk", updated.Name);
            Assert.Equal(2, updated.Quantity);
            Assert.Equal("pantry", updated.Location);

            var fetched = await _client.GetFromJsonAsync<PantryItemDto>($"/api/pantry/{created.Id}");
            Assert.NotNull(fetched);
            Assert.Equal("Oat milk", fetched.Name);
        }
        finally
        {
            await DeleteItemsByNameAsync(name);
        }
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/pantry/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Existing_RemovesItem()
    {
        var name = $"test-item-{Guid.CreateVersion7()}";
        var created = await CreateItemAsync(name);

        var delete = await _client.DeleteAsync($"/api/pantry/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var fetch = await _client.GetAsync($"/api/pantry/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    private async Task<PantryItemDto> CreateItemAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/pantry", new CreatePantryItemRequest(
            name, 1, "L", "fridge", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(created);
        return created;
    }

    private async Task DeleteItemsByNameAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Items.Where(x => x.Name == name).ExecuteDeleteAsync();
    }
}
