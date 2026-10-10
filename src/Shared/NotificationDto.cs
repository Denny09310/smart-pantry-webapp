namespace Shared.Models;

public record NotificationDto(
    string Id,
    string PantryItemId,
    string ItemName,
    DateOnly ExpirationDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);