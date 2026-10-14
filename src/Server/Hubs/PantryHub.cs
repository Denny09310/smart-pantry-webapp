using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;

using Server.Services;

using Shared.Models;

namespace Server.Hubs;

/// <summary>
/// Typed pantry hub: one broadcast channel for the whole household.
/// Clients re-query the endpoints on broadcast; the hub never carries item data.
/// Member presence (one member id per connection via ?memberId=) is broadcast
/// on every connect and disconnect; unknown ids stay anonymous.
/// </summary>
public sealed class PantryHub(PresenceTracker presence, IServiceScopeFactory scopes) : Hub<IPantryHubClient>
{
    public override async Task OnConnectedAsync()
    {
        var memberId = await ResolveMemberIdAsync();
        await Clients.All.PresenceChanged(presence.Connect(Context.ConnectionId, memberId), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.All.PresenceChanged(presence.Disconnect(Context.ConnectionId), CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<string?> ResolveMemberIdAsync()
    {
        var candidate = Context.GetHttpContext()?.Request.Query["memberId"].ToString();
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var known = await db.Members.AsNoTracking()
            .AnyAsync(m => m.Id == candidate && !m.IsArchived);

        return known ? candidate : null;
    }
}
