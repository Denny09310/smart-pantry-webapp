using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;

using Shared.Models;

namespace Server.Tests;

public class CosmeticApiTests(CosmeticApiFactory factory) : IClassFixture<CosmeticApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CosmeticApiFactory _factory = factory;

    [Fact]
    public async Task Get_ReturnsOk_WithExpectedShape()
    {
        var response = await _client.GetAsync("/api/cosmetics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCosmeticItemsResponse>();
        Assert.NotNull(body);
        Assert.True(body.TotalItems >= 0);
        Assert.NotNull(body.Items);
    }

    [Fact]
    public async Task Get_TakeAboveMaximum_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/cosmetics?take=200");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("Take", out _));
    }

    [Fact]
    public async Task Post_InvalidRequest_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/cosmetics", new CreateCosmeticItemRequest(
            "", -1, "L", "face", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out _));
        Assert.True(errors.TryGetProperty("Quantity", out _));
    }

    [Fact]
    public async Task Post_WithMemberHeader_StampsItem()
    {
        var member = await CreateMemberAsync($"test-member-{Guid.CreateVersion7()}");

        try
        {
            var name = $"test-milk-{Guid.CreateVersion7()}";
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/cosmetics")
            {
                Content = JsonContent.Create(new CreateCosmeticItemRequest(
                    name, 1, "L", "face", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))))
            };
            request.Headers.Add("X-Member-Id", member.Id);

            var response = await _client.SendAsync(request);

            try
            {
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var created = await response.Content.ReadFromJsonAsync<CosmeticItemDto>();
                Assert.NotNull(created);
                Assert.Equal(member.Id, created.HouseholdMemberId);
            }
            finally
            {
                await CosmeticTestHelpers.DeleteItemsByNameAsync(_factory.Services, name);
            }
        }
        finally
        {
            await CosmeticTestHelpers.DeleteMemberAsync(_factory.Services, member.Id);
        }
    }

    [Fact]
    public async Task Post_WithUnknownMemberHeader_CreatesUnattributedItem()
    {
        var name = $"test-milk-{Guid.CreateVersion7()}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/cosmetics")
        {
            Content = JsonContent.Create(new CreateCosmeticItemRequest(
                name, 1, "L", "face", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))))
        };
        request.Headers.Add("X-Member-Id", "does-not-exist");

        var response = await _client.SendAsync(request);

        try
        {
            // Attribution is a label, never access control: unknown members resolve to null.
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<CosmeticItemDto>();
            Assert.NotNull(created);
            Assert.Null(created.HouseholdMemberId);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemsByNameAsync(_factory.Services, name);
        }
    }

    [Fact]
    public async Task Post_ValidRequest_CreatesItem_VisibleInList()
    {
        var name = $"test-milk-{Guid.CreateVersion7()}";

        var create = await _client.PostAsJsonAsync("/api/cosmetics", new CreateCosmeticItemRequest(
            name, 1, "L", "face", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));

        try
        {
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<CosmeticItemDto>();
            Assert.NotNull(created);
            Assert.False(string.IsNullOrWhiteSpace(created.Id));
            Assert.Equal(name, created.Name);

            var list = await _client.GetFromJsonAsync<GetCosmeticItemsResponse>(
                $"/api/cosmetics?name={Uri.EscapeDataString(name)}");
            Assert.NotNull(list);
            Assert.Contains(list.Items, x => x.Id == created.Id);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemsByNameAsync(_factory.Services, name);
        }
    }

    [Fact]
    public async Task Post_MalformedExpiryDate_ReturnsBadRequest()
    {
        using var content = new StringContent(
            """{"name":"Milk","quantity":1,"unit":"L","category":"face","expiryDate":"not-a-date"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await _client.PostAsync("/api/cosmetics", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFoundProblem()
    {
        var response = await _client.GetAsync($"/api/cosmetics/{Guid.CreateVersion7()}");

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
            var response = await _client.GetAsync($"/api/cosmetics/{created.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var item = await response.Content.ReadFromJsonAsync<CosmeticItemDto>();
            Assert.NotNull(item);
            Assert.Equal(created.Id, item.Id);
            Assert.Equal(name, item.Name);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemsByNameAsync(_factory.Services, name);
        }
    }

    [Fact]
    public async Task Put_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/cosmetics/{Guid.CreateVersion7()}",
            new UpdateCosmeticItemRequest("Milk", 1, "L", "face", null,
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
            var response = await _client.PutAsJsonAsync($"/api/cosmetics/{created.Id}",
                new UpdateCosmeticItemRequest("", -1, "L", "face", null,
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemsByNameAsync(_factory.Services, name);
        }
    }

    [Fact]
    public async Task Put_ValidRequest_UpdatesItem()
    {
        var name = $"test-item-{Guid.CreateVersion7()}";
        var created = await CreateItemAsync(name);

        try
        {
            var response = await _client.PutAsJsonAsync($"/api/cosmetics/{created.Id}",
                new UpdateCosmeticItemRequest("Oat milk", 2, "L", "body", "updated",
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<CosmeticItemDto>();
            Assert.NotNull(updated);
            Assert.Equal("Oat milk", updated.Name);
            Assert.Equal(2, updated.Quantity);
            Assert.Equal("body", updated.Category);

            var fetched = await _client.GetFromJsonAsync<CosmeticItemDto>($"/api/cosmetics/{created.Id}");
            Assert.NotNull(fetched);
            Assert.Equal("Oat milk", fetched.Name);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemsByNameAsync(_factory.Services, name);
        }
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/cosmetics/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Existing_RemovesItem()
    {
        var name = $"test-item-{Guid.CreateVersion7()}";
        var created = await CreateItemAsync(name);

        var delete = await _client.DeleteAsync($"/api/cosmetics/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var fetch = await _client.GetAsync($"/api/cosmetics/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    private async Task<CosmeticItemDto> CreateItemAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/cosmetics", new CreateCosmeticItemRequest(
            name, 1, "L", "face", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CosmeticItemDto>();
        Assert.NotNull(created);
        return created;
    }

    private async Task<HouseholdMemberDto> CreateMemberAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync("/api/members",
            new CreateHouseholdMemberRequest(displayName));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<HouseholdMemberDto>();
        Assert.NotNull(created);
        return created;
    }

}