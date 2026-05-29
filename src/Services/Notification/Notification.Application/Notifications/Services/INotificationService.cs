using Notification.Application.Notifications.Dto;
using TournamentPlatform.Shared.Common;

namespace Notification.Application.Notifications.Services;

public interface INotificationService
{
    Task<NotificationListResponse> ListAsync(
        Guid recipientUserId,
        bool unreadOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result> MarkReadAsync(
        Guid notificationId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default);
}
