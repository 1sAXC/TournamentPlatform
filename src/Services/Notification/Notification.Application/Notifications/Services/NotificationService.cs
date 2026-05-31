using Notification.Application.Notifications.Abstractions;
using Notification.Application.Notifications.Dto;
using TournamentPlatform.Shared.Common;

namespace Notification.Application.Notifications.Services;

public sealed class NotificationService(INotificationRepository notifications) : INotificationService
{
    public async Task<NotificationListResponse> ListAsync(
        Guid recipientUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var skip = Math.Max(0, (pageNumber - 1) * pageSize);
        var take = Math.Clamp(pageSize, 1, 100);

        var items = await notifications.GetForUserAsync(
            recipientUserId,
            skip,
            take,
            cancellationToken);

        var total = await notifications.CountForUserAsync(recipientUserId, cancellationToken);
        var unread = await notifications.CountUnreadForUserAsync(recipientUserId, cancellationToken);

        var responseItems = items
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.Type.ToString(),
                notification.Title,
                notification.Body,
                notification.LinkUrl,
                notification.PayloadJson,
                notification.CreatedAtUtc,
                notification.ReadAtUtc))
            .ToArray();

        return new NotificationListResponse(
            responseItems,
            total,
            unread,
            pageNumber <= 0 ? 1 : pageNumber,
            take);
    }

    public async Task<Result> MarkReadAsync(
        Guid notificationId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        var notification = await notifications.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null)
        {
            return Result.Failure(NotificationErrors.NotFound);
        }

        if (notification.RecipientUserId != recipientUserId)
        {
            return Result.Failure(NotificationErrors.AccessDenied);
        }

        notification.MarkRead(DateTime.UtcNow);
        await notifications.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
