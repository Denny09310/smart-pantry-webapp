using Microsoft.AspNetCore.SignalR;

using Shared.Models;

namespace Server.Hubs;

/// <summary>
/// Typed pantry hub: one broadcast channel for the whole household.
/// Clients re-query the endpoints on broadcast; the hub never carries item data.
/// </summary>
public sealed class PantryHub : Hub<IPantryHubClient>;
