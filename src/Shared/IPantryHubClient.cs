namespace Shared.Models;

/// <summary>
/// Typed SignalR client contract for pantry broadcasts, shared by the
/// server hub (<c>Hub&lt;IPantryHubClient&gt;</c>) and the Blazor client
/// (subscribed via <c>nameof(IPantryHubClient.PantryChanged)</c>, so a
/// rename breaks the build instead of silently dropping events).
/// </summary>
public interface IPantryHubClient
{
    Task PantryChanged(PantryChangedEvent evt);
}
