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
    public static async Task<Results<Ok<PushSubscriptionDto>, ValidationProblem>> SubscribeAsync(
        PushSubscriptionRequest request,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var entry = await db.PushSubscriptions
            .FirstOrDefaultAsync(x => x.Endpoint == request.Endpoint, ct);

        if (entry is null)
        {
            entry = new PushSubscription
            {
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth
            };
            db.PushSubscriptions.Add(entry);
        }
        else
        {
            entry.P256dh = request.P256dh;
            entry.Auth = request.Auth;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new PushSubscriptionDto(entry.Id, entry.Endpoint, entry.CreatedAt));
    }

    [MapDelete("/subscriptions")]
    public static async Task<NoContent> UnsubscribeAsync(
        string endpoint,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        await db.PushSubscriptions
            .Where(x => x.Endpoint == endpoint)
            .ExecuteDeleteAsync(ct);

        return TypedResults.NoContent();
    }
}