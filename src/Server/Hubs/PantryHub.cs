using Microsoft.AspNetCore.SignalR;

using Shared.Models;

namespace Server.Hubs;

/// <summary>
/// Broadcast-only hub for pantry changes: one channel for the whole
/// household (no groups, no authorization — same trust model as the
/// REST endpoints). Clients implement <see cref="IPantryHubClient"/> and
/// re-query on <c>PantryChanged</c>; broadcasts are progressive
/// enhancement, never required for correctness.
/// </summary>
public sealed class PantryHub : Hub<IPantryHubClient>
{
}
