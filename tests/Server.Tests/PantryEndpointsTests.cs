using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Models;

namespace Server.Tests;

[Collection("api")]
public sealed class PantryEndpointsTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueName(string prefix)
        => $"{prefix}-{Guid.NewGuid():N}";

    private static DateOnly InDays(int days)
        => DateOnly.FromDateTime(DateTime.Today).AddDays(days);

    private async Task<PantryItemDto> CreateItemAsync(
        string? name = null,
        DateOnly? expires = null)
    {
        var request = new CreatePantryItemRequest(
            name ?? UniqueName("item"),
            1,
            "pcs",
            "pantry",
            null,
            expires ?? InDays(30));

        using var response = await _client.PostAsJsonAsync("/api/pantry", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(created);
        return created;
    }

    private async Task DeleteItemAsync(string id)
    {
        using var response = await _client.DeleteAsync($"/api/pantry/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Create_GetById_Update_Delete_Roundtrip()
    {
        var name = UniqueName("roundtrip");
        var created = await CreateItemAsync(name, InDays(3));
        try
        {
            Assert.Equal(name, created.Name);
            Assert.Equal(ExpiryStatus.Soon, created.Status);

            using var get = await _client.GetAsync($"/api/pantry/{created.Id}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var fetched = await get.Content.ReadFromJsonAsync<PantryItemDto>();
            Assert.NotNull(fetched);
            Assert.Equal(created.Id, fetched.Id);

            var update = new UpdatePantryItemRequest(name, 5, "pcs", "fridge", "shelf", InDays(3));
            using var put = await _client.PutAsJsonAsync($"/api/pantry/{created.Id}", update);
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            var updated = await put.Content.ReadFromJsonAsync<PantryItemDto>();
            Assert.NotNull(updated);
            Assert.Equal(5, updated.Quantity);
            Assert.Equal("fridge", updated.Location);
        }
        finally
        {
            await DeleteItemAsync(created.Id);
        }

        using var missing = await _client.GetAsync($"/api/pantry/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Create_Rejects_Invalid_Item()
    {
        var request = new CreatePantryItemRequest(string.Empty, 0, string.Empty, string.Empty, null, InDays(1));

        using var response = await _client.PostAsJsonAsync("/api/pantry", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var errors = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(errors);
        var fields = errors.RootElement.GetProperty("errors");
        Assert.True(fields.TryGetProperty("Name", out _));
        Assert.True(fields.TryGetProperty("Quantity", out _));
        Assert.True(fields.TryGetProperty("Unit", out _));
        Assert.True(fields.TryGetProperty("Location", out _));
    }

    [Fact]
    public async Task Update_Missing_Returns_NotFound()
    {
        var request = new UpdatePantryItemRequest("ghost", 1, "pcs", "pantry", null, InDays(3));

        using var response = await _client.PutAsJsonAsync($"/api/pantry/{Guid.NewGuid():N}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Missing_Returns_NotFound()
    {
        using var response = await _client.DeleteAsync($"/api/pantry/{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_Clamps_Paging()
    {
        // Negative skip is clamped instead of blowing up; take is capped.
        using var negative = await _client.GetAsync("/api/pantry?skip=-5&take=10");
        Assert.Equal(HttpStatusCode.OK, negative.StatusCode);

        using var huge = await _client.GetAsync("/api/pantry?take=1000000");
        Assert.Equal(HttpStatusCode.OK, huge.StatusCode);
        var page = await huge.Content.ReadFromJsonAsync<GetPantryItemsResponse>();
        Assert.NotNull(page);
        Assert.True(page.Items.Count() <= 100);
    }

    [Fact]
    public async Task Search_Matches_Substring()
    {
        var match = await CreateItemAsync($"{UniqueName("oat")}milk");
        var other = await CreateItemAsync(UniqueName("juice"));
        try
        {
            using var response = await _client.GetAsync("/api/pantry?name=oat&take=500");
            response.EnsureSuccessStatusCode();
            var page = await response.Content.ReadFromJsonAsync<GetPantryItemsResponse>();
            Assert.NotNull(page);

            var ids = page.Items.Select(i => i.Id).ToList();
            Assert.Contains(match.Id, ids);
            Assert.DoesNotContain(other.Id, ids);
        }
        finally
        {
            await DeleteItemAsync(match.Id);
            await DeleteItemAsync(other.Id);
        }
    }
    [Fact]
    public async Task List_Supports_Status_Filter()
    {
        var soon = await CreateItemAsync(expires: InDays(2));
        var fresh = await CreateItemAsync(expires: InDays(60));
        try
        {
            using var response = await _client.GetAsync("/api/pantry?status=Soon&take=500");
            response.EnsureSuccessStatusCode();
            var page = await response.Content.ReadFromJsonAsync<GetPantryItemsResponse>();
            Assert.NotNull(page);

            var ids = page.Items.Select(i => i.Id).ToList();
            Assert.Contains(soon.Id, ids);
            Assert.DoesNotContain(fresh.Id, ids);
            Assert.All(page.Items, i => Assert.Equal(ExpiryStatus.Soon, i.Status));
        }
        finally
        {
            await DeleteItemAsync(soon.Id);
            await DeleteItemAsync(fresh.Id);
        }
    }
}
