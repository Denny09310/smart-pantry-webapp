using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;

using Shared.Models;

namespace Server.Tests;

public class MemberApiTests(CosmeticApiFactory factory) : IClassFixture<CosmeticApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedMember()
    {
        var name = $"test-member-{Guid.CreateVersion7()}";
        var response = await _client.PostAsJsonAsync("/api/members",
            new CreateHouseholdMemberRequest(name, "#A3B18A"));

        try
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<HouseholdMemberDto>();
            Assert.NotNull(created);
            Assert.False(string.IsNullOrWhiteSpace(created.Id));
            Assert.Equal(name, created.DisplayName);
            Assert.Equal("#A3B18A", created.Color);
            Assert.False(created.IsArchived);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteMembersByNameAsync(factory.Services, name);
        }
    }

    [Fact]
    public async Task Create_MissingName_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/members",
            new CreateHouseholdMemberRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("DisplayName", out _));
    }

    [Fact]
    public async Task Create_WhitespaceName_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/members",
            new CreateHouseholdMemberRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("DisplayName", out _));
    }

    [Fact]
    public async Task Update_RenameAndArchive_RoundTrips()
    {
        var created = await CreateMemberAsync($"test-member-{Guid.CreateVersion7()}");

        try
        {
            var renamed = $"renamed-{Guid.CreateVersion7()}";
            var update = await _client.PutAsJsonAsync($"/api/members/{created.Id}",
                new UpdateHouseholdMemberRequest(renamed, null, true));

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var updated = await update.Content.ReadFromJsonAsync<HouseholdMemberDto>();
            Assert.NotNull(updated);
            Assert.Equal(renamed, updated.DisplayName);
            Assert.Null(updated.Color);
            Assert.True(updated.IsArchived);

            var list = await _client.GetFromJsonAsync<List<HouseholdMemberDto>>("/api/members");
            Assert.NotNull(list);
            Assert.Contains(list, m => m.Id == created.Id && m.IsArchived);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteMemberAsync(factory.Services, created.Id);
        }
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/members/{Guid.CreateVersion7()}",
            new UpdateHouseholdMemberRequest("Ghost", null, false));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ArchivedMember_RemovesItAndNullsAttribution()
    {
        var created = await CreateMemberAsync($"test-member-{Guid.CreateVersion7()}");

        var itemName = $"test-item-{Guid.CreateVersion7()}";
        using var itemRequest = new HttpRequestMessage(HttpMethod.Post, "/api/cosmetics")
        {
            Content = JsonContent.Create(new CreateCosmeticItemRequest(
                itemName, 1, "pcs", "body", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))))
        };
        itemRequest.Headers.Add("X-Member-Id", created.Id);
        using var itemResponse = await _client.SendAsync(itemRequest);
        itemResponse.EnsureSuccessStatusCode();

        try
        {
            using var archive = await _client.PutAsJsonAsync($"/api/members/{created.Id}",
                new UpdateHouseholdMemberRequest(created.DisplayName, created.Color, true));
            archive.EnsureSuccessStatusCode();

            using var delete = await _client.DeleteAsync($"/api/members/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var list = await _client.GetFromJsonAsync<List<HouseholdMemberDto>>("/api/members");
            Assert.NotNull(list);
            Assert.DoesNotContain(list, m => m.Id == created.Id);

            var items = await _client.GetFromJsonAsync<GetCosmeticItemsResponse>(
                $"/api/cosmetics?name={Uri.EscapeDataString(itemName)}");
            Assert.NotNull(items);
            var item = Assert.Single(items.Items);
            Assert.Null(item.HouseholdMemberId);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteItemsByNameAsync(factory.Services, itemName);
            await CosmeticTestHelpers.DeleteMemberAsync(factory.Services, created.Id);
        }
    }

    [Fact]
    public async Task Delete_ActiveMember_ReturnsConflict()
    {
        var created = await CreateMemberAsync($"test-member-{Guid.CreateVersion7()}");

        try
        {
            using var response = await _client.DeleteAsync($"/api/members/{created.Id}");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
        finally
        {
            await CosmeticTestHelpers.DeleteMemberAsync(factory.Services, created.Id);
        }
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        using var response = await _client.DeleteAsync($"/api/members/{Guid.CreateVersion7()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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