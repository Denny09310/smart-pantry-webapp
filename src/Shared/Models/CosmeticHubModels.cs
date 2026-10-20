using System.Text.Json.Serialization;

namespace Shared.Models;

/// <summary>Kinds of cosmetics change broadcast over the realtime hub.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CosmeticChangeKind>))]
public enum CosmeticChangeKind
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
/// A cosmetics change broadcast: the item id plus what happened.
/// Payloads stay tiny on purpose — clients fetch the single item and patch
/// their lists, falling back to a full re-query when anything is uncertain.
/// </summary>
public sealed record CosmeticChangedEvent(string ItemId, CosmeticChangeKind ChangeKind);

/// <summary>Typed SignalR client surface for the cosmetics hub.</summary>
public interface ICosmeticHubClient
{
    Task CosmeticChanged(CosmeticChangedEvent change, CancellationToken ct);

    Task PresenceChanged(IReadOnlyList<string> onlineMemberIds, CancellationToken ct);
}