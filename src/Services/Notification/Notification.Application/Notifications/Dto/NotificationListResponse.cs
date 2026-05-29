namespace Notification.Application.Notifications.Dto;

public sealed record NotificationListResponse(
    IReadOnlyCollection<NotificationResponse> Items,
    int TotalCount,
    int UnreadCount,
    int PageNumber,
    int PageSize);
