using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Application.Notifications.Abstractions;

public interface INotificationRepository
{
    Task<IReadOnlyCollection<NotificationEntity>> GetForUserAsync(
        Guid recipientUserId,
        bool unreadOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountForUserAsync(
        Guid recipientUserId,
        bool unreadOnly,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadForUserAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default);

    Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForSourceEventAsync(
        Guid sourceEventId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default);

    void Add(NotificationEntity notification);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
