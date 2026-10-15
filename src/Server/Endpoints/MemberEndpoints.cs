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
    public async Task<Ok<List<HouseholdMemberDto>>> GetMembersAsync(CancellationToken ct)
    {
        var members = await db.Members
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => new HouseholdMemberDto(
                x.Id,
                x.DisplayName,
                x.Color,
                x.IsArchived))
            .ToListAsync(ct);

        return TypedResults.Ok(members);
    }

    [MapPost("/")]
    public async Task<Results<Created<HouseholdMemberDto>, ValidationProblem>> CreateMemberAsync(
        CreateHouseholdMemberRequest request,
        CancellationToken ct)
    {
        // [Required] passes whitespace: reject it before Trim stores "".
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.DisplayName)] = ["Display name must not be blank."]
            });
        }

        var entry = new HouseholdMember
        {
            DisplayName = request.DisplayName.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim()
        };

        db.Members.Add(entry);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/members/{entry.Id}", ToDto(entry));
    }

    [MapPut("/{id}")]
    public async Task<Results<Ok<HouseholdMemberDto>, ProblemHttpResult, ValidationProblem>> UpdateMemberAsync(
        string id,
        UpdateHouseholdMemberRequest request,
        CancellationToken ct)
    {
        var entry = await db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null)
            return MemberNotFound(id);

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.DisplayName)] = ["Display name must not be blank."]
            });
        }

        entry.DisplayName = request.DisplayName.Trim();
        entry.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim();
        entry.IsArchived = request.IsArchived;
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToDto(entry));
    }

    [MapDelete("/{id}")]
    public async Task<Results<NoContent, ProblemHttpResult>> DeleteMemberAsync(
        string id,
        CancellationToken ct)
    {
        // Atomic: only archived members are deleted (items keep working, their
        // attribution is nulled by the FK SetNull, never cascade-deleted).
        // Zero rows means unknown id or active member — disambiguate below.
        var deleted = await db.Members
            .Where(x => x.Id == id && x.IsArchived)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            return TypedResults.NoContent();

        var exists = await db.Members.AsNoTracking().AnyAsync(x => x.Id == id, ct);

        return exists
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"Household member '{id}' must be archived before deleting it.")
            : MemberNotFound(id);
    }

    private static ProblemHttpResult MemberNotFound(string id)
        => EndpointProblems.NotFound("Household member", id);

    private static HouseholdMemberDto ToDto(HouseholdMember member) => new(
        member.Id,
        member.DisplayName,
        member.Color,
        member.IsArchived);
}