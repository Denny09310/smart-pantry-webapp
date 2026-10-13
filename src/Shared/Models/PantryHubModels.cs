using System.Text.Json.Serialization;

namespace Shared.Models;

/// <summary>Kinds of pantry change broadcast over the realtime hub.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PantryChangeKind>))]
public enum PantryChangeKind
{
    /// <summary>A new item was created.</summary>
    [JsonStringEnumMemberName("created")]
    Created,

    /// <summary>An existing item was updated.</summary>
    [JsonStringEnumMemberName("updated")]
    Updated,

    /// <summary>An item was removed.</summary>
    [JsonStringEnumMemberName("removed")]
    Removed,
}

/// <summary>
/// A pantry change broadcast: the item id plus what happened.
/// Payloads stay tiny on purpose — clients re-query the endpoints.
/// </summary>
public sealed record PantryChangedEvent(string ItemId, PantryChangeKind ChangeKind);

/// <summary>Typed SignalR client surface for the pantry hub.</summary>
public interface IPantryHubClient
{
    Task PantryChanged(PantryChangedEvent change, CancellationToken ct);
}
