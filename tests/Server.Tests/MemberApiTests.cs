using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Data;
using Shared.Models;

namespace Server.Tests;

public class MemberApiTests(PantryApiFactory factory) : IClassFixture<PantryApiFactory>
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
            await CleanupMemberByNameAsync(name);
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
            await CleanupMemberByIdAsync(created.Id);
        }
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/members/{Guid.CreateVersion7()}",
            new UpdateHouseholdMemberRequest("Ghost", null, false));

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

    private async Task CleanupMemberByNameAsync(string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Members.Where(x => x.DisplayName == name).ExecuteDeleteAsync();
    }

    private async Task CleanupMemberByIdAsync(string id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Members.Where(x => x.Id == id).ExecuteDeleteAsync();
    }
}
