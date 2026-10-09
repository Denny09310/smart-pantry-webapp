namespace Shared.Models;

public record GetNotificationsResponse(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount);
