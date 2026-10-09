using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Data.Entities;

using Shared.Models;

namespace Server.Endpoints;

[Tags("Members")]
[MapGroup("/api/members")]
internal class MemberEndpoints(ApplicationDbContext db)
{
    [MapGet("/")]
    public async Task<Ok<List<MemberDto>>> GetMembersAsync(CancellationToken ct)
    {
        var members = await db.Members
            .AsNoTracking()
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Select(m => new MemberDto(
                m.Id,
                m.Name,
                m.Color,
                m.CreatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(members);
    }

    [MapPost("/")]
    public async Task<Results<Created<MemberDto>, ValidationProblem>> CreateMemberAsync(
        CreateMemberRequest request,
        CancellationToken ct)
    {
        var entry = new Member
        {
            Name = request.Name.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim(),
        };

        db.Members.Add(entry);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/members/{entry.Id}", ToDto(entry));
    }

    [MapDelete("/{id}")]
    public async Task<Results<NoContent, NotFound>> DeleteMemberAsync(
        string id,
        CancellationToken ct)
    {
        var entry = await db.Members.FindAsync([id], ct);

        if (entry is null)
            return TypedResults.NotFound();

        db.Members.Remove(entry);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static MemberDto ToDto(Member entry)
        => new(
            entry.Id,
            entry.Name,
            entry.Color,
            entry.CreatedAt);
}
