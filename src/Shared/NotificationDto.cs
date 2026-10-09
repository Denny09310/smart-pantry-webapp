namespace Shared.Models;

public record NotificationDto(
    string Id,
    string PantryItemId,
    string ItemName,
    string Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);