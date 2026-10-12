namespace Shared.Models;

/// <summary>Kinds of pantry change broadcast over the realtime hub.</summary>
public static class PantryChangeKinds
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Removed = "removed";
}

/// <summary>
/// A pantry change broadcast: the item id plus what happened.
/// Payloads stay tiny on purpose — clients re-query the endpoints.
/// </summary>
public sealed record PantryChangedEvent(string ItemId, string ChangeKind);

/// <summary>Typed SignalR client surface for the pantry hub.</summary>
public interface IPantryHubClient
{
    Task PantryChanged(PantryChangedEvent change, CancellationToken ct);
}
