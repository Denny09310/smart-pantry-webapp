using System.Net;
using System.Net.Http.Json;

using Shared.Models;

namespace Server.Tests;

[Collection("api")]
public sealed class MemberEndpointsTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueName(string prefix)
        => $"{prefix}-{Guid.NewGuid():N}"[..20];

    [Fact]
    public async Task Create_Then_List_Contains_Member()
    {
        var name = UniqueName("member");

        using var created = await _client.PostAsJsonAsync(
            "/api/members",
            new CreateMemberRequest(name, null));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var member = await created.Content.ReadFromJsonAsync<MemberDto>();
        Assert.NotNull(member);
        Assert.Equal(name, member.Name);
        Assert.Null(member.Color);

        try
        {
            using var listed = await _client.GetAsync("/api/members");
            listed.EnsureSuccessStatusCode();

            var members = await listed.Content.ReadFromJsonAsync<List<MemberDto>>();
            Assert.NotNull(members);
            Assert.Contains(members, m => m.Id == member.Id);
        }
        finally
        {
            await _client.DeleteAsync($"/api/members/{member.Id}");
        }
    }

    [Fact]
    public async Task Create_Rejects_Invalid_Member()
    {
        using var empty = await _client.PostAsJsonAsync(
            "/api/members",
            new CreateMemberRequest(string.Empty, null));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        using var tooLong = await _client.PostAsJsonAsync(
            "/api/members",
            new CreateMemberRequest(new string('x', 31), null));
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    [Fact]
    public async Task Delete_Missing_Returns_NotFound()
    {
        using var response = await _client.DeleteAsync($"/api/members/{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Item_With_Member_Header_Sets_Attribution()
    {
        var member = await CreateMemberAsync();
        try
        {
            var item = await CreateItemAsync(member.Id);
            try
            {
                // The write response carries the id; the name resolves on reads.
                Assert.Equal(member.Id, item.CreatedByMemberId);

                using var fetched = await _client.GetAsync($"/api/pantry/{item.Id}");
                fetched.EnsureSuccessStatusCode();

                var reread = await fetched.Content.ReadFromJsonAsync<PantryItemDto>();
                Assert.NotNull(reread);
                Assert.Equal(member.Id, reread.CreatedByMemberId);
                Assert.Equal(member.Name, reread.CreatedByName);
            }
            finally
            {
                await _client.DeleteAsync($"/api/pantry/{item.Id}");
            }
        }
        finally
        {
            await _client.DeleteAsync($"/api/members/{member.Id}");
        }
    }

    [Fact]
    public async Task Create_Item_With_Unknown_Member_Ignores_Header()
    {
        var item = await CreateItemAsync(Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Null(item.CreatedByMemberId);
            Assert.Null(item.CreatedByName);
        }
        finally
        {
            await _client.DeleteAsync($"/api/pantry/{item.Id}");
        }
    }

    [Fact]
    public async Task Delete_Member_Keeps_Items_With_Nulled_Attribution()
    {
        var member = await CreateMemberAsync();
        var item = await CreateItemAsync(member.Id);

        using var deleted = await _client.DeleteAsync($"/api/members/{member.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        try
        {
            using var fetched = await _client.GetAsync($"/api/pantry/{item.Id}");
            fetched.EnsureSuccessStatusCode();

            var reread = await fetched.Content.ReadFromJsonAsync<PantryItemDto>();
            Assert.NotNull(reread);
            Assert.Null(reread.CreatedByMemberId);
            Assert.Null(reread.CreatedByName);
        }
        finally
        {
            await _client.DeleteAsync($"/api/pantry/{item.Id}");
        }
    }

    private async Task<MemberDto> CreateMemberAsync()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/members",
            new CreateMemberRequest(UniqueName("member"), null));
        response.EnsureSuccessStatusCode();

        var member = await response.Content.ReadFromJsonAsync<MemberDto>();
        Assert.NotNull(member);
        return member;
    }

    private async Task<PantryItemDto> CreateItemAsync(string? memberId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/pantry")
        {
            Content = JsonContent.Create(new CreatePantryItemRequest(
                UniqueName("item"),
                1,
                "pcs",
                "pantry",
                null,
                DateOnly.FromDateTime(DateTime.Today).AddDays(30))),
        };

        if (memberId is not null)
            request.Headers.Add("X-Member-Id", memberId);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var item = await response.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(item);
        return item;
    }
}
