using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Server.Data;
using Server.Data.Entities;
using Server.Services;

using Shared.Models;

namespace Server.Endpoints;

[Tags("Push")]
[MapGroup("/api/push")]
internal class PushEndpoints(ApplicationDbContext db, IOptions<PushOptions> options)
{
    [MapGet("/vapid-public-key")]
    public Results<Ok<PushPublicKeyResponse>, NotFound> GetPublicKey()
        => string.IsNullOrWhiteSpace(options.Value.PublicKey)
            ? TypedResults.NotFound()
            : TypedResults.Ok(new PushPublicKeyResponse(options.Value.PublicKey));

    [MapPost("/subscriptions")]
    public async Task<Results<Ok<PushSubscriptionDto>, ValidationProblem>> SubscribeAsync(
        PushSubscriptionRequest request,
        CancellationToken ct)
    {
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);

        if (existing is not null)
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(ToDto(existing));
        }

        var entry = new PushSubscription
        {
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth,
        };

        db.PushSubscriptions.Add(entry);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToDto(entry));
    }

    [MapDelete("/subscriptions")]
    public async Task<Results<NoContent, ValidationProblem>> UnsubscribeAsync(
        [AsParameters] UnsubscribePushRequest request,
        CancellationToken ct)
    {
        await db.PushSubscriptions
            .Where(s => s.Endpoint == request.Endpoint)
            .ExecuteDeleteAsync(ct);

        return TypedResults.NoContent();
    }

    private static PushSubscriptionDto ToDto(PushSubscription entry)
        => new(entry.Id, entry.Endpoint, entry.CreatedAt);
}