namespace Shared.Models;

/// <summary>
/// Broadcast when a pantry item is created, updated, or removed.
/// Payloads stay tiny on purpose: receivers re-query the endpoints,
/// which remain the source of truth.
/// </summary>
public record PantryChangedEvent(string Kind, string? ItemId)
{
    public const string Created = "created";

    public const string Updated = "updated";

    public const string Removed = "removed";
}
