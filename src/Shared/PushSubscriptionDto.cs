namespace Shared.Models;

public record PushSubscriptionDto(
    string Id,
    string Endpoint,
    DateTimeOffset CreatedAt);
