namespace Shared.Models;

public record NotificationDto(
    string Id,
    string CosmeticItemId,
    string ItemName,
    DateOnly ExpiryDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public record GetNotificationsResponse(
    List<NotificationDto> Items,
    int UnreadCount,
    int TotalCount);