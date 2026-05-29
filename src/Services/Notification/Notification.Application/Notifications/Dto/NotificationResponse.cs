namespace Notification.Application.Notifications.Dto;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string LinkUrl,
    string PayloadJson,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
