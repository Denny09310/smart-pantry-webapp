using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Server.Data;
using Server.Data.Entities;
using Server.Models;

using Shared.Models;

namespace Server.Endpoints;

[Tags("Push")]
[MapGroup("/api/push")]
internal static class PushEndpoints
{
    [MapGet("/public-key")]
    public static Results<Ok<GetPushNotificationKey>, ProblemHttpResult> GetPushNotificationKeyAsync(
        IOptions<VapidOptions> options)
    {
        var key = options.Value.PublicKey;

        return string.IsNullOrWhiteSpace(key)
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: "Push notifications are not configured.")
            : TypedResults.Ok(new GetPushNotificationKey(key));
    }

    [MapPost("/subscriptions")]
    public static async Task<Results<Created<PushSubscriptionDto>, Ok<PushSubscriptionDto>, ValidationProblem>> SubscribeAsync(
        PushSubscriptionRequest request,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // One atomic upsert. The browser re-registers its subscription on every
        // load, so this endpoint is hit repeatedly for the same endpoint and can
        // race a concurrent unsubscribe. A read-then-write lost that race: the
        // row was found, deleted by the other request, then the UPDATE matched
        // nothing and the whole subscribe failed with a 500 — leaving the
        // browser believing it is subscribed while the server had no row at all.
        // INSERT ... ON CONFLICT cannot lose, and xmax = 0 tells the insert and
        // the update apart so the 201/200 contract is preserved.
        var rows = await db.Database
            .SqlQuery<PushSubscriptionRow>($"""
                INSERT INTO push_subscriptions AS p (id, endpoint, p256dh, auth, created_at, updated_at)
                VALUES ({Guid.CreateVersion7().ToString()}, {request.Endpoint}, {request.P256dh}, {request.Auth}, {now}, NULL)
                ON CONFLICT (endpoint) DO UPDATE
                SET p256dh = EXCLUDED.p256dh,
                    auth = EXCLUDED.auth,
                    updated_at = EXCLUDED.created_at
                RETURNING id, created_at, (xmax = 0) AS inserted
                """)
            .ToListAsync(ct);

        var row = rows[0];
        var dto = new PushSubscriptionDto(row.Id, request.Endpoint, row.CreatedAt);

        return row.Inserted
            ? TypedResults.Created($"/api/push/subscriptions/{row.Id}", dto)
            : TypedResults.Ok(dto);
    }

    /// <summary>
    /// Unmapped result of the upsert above. The snake_case naming convention
    /// applies to it too, so the RETURNING aliases are snake_case as well.
    /// </summary>
    private sealed record PushSubscriptionRow(string Id, DateTimeOffset CreatedAt, bool Inserted);

    [MapDelete("/subscriptions")]
    public static async Task<Results<NoContent, ValidationProblem>> UnsubscribeAsync(
        string? endpoint,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(endpoint)] = ["Endpoint must not be blank."]
            });
        }

        await db.PushSubscriptions
            .Where(x => x.Endpoint == endpoint)
            .ExecuteDeleteAsync(ct);

        return TypedResults.NoContent();
    }
}