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
}
