using Microsoft.EntityFrameworkCore;
using Notification.Application.Notifications.Abstractions;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(NotificationDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyCollection<NotificationEntity>> GetForUserAsync(
        Guid recipientUserId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountForUserAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId)
            .CountAsync(cancellationToken);
    }

    public Task<int> CountUnreadForUserAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId && notification.ReadAtUtc == null)
            .CountAsync(cancellationToken);
    }

    public Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Notifications.FirstOrDefaultAsync(notification => notification.Id == id, cancellationToken);
    }

    public Task<bool> ExistsForSourceEventAsync(
        Guid sourceEventId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Notifications.AnyAsync(
            notification => notification.SourceEventId == sourceEventId
                && notification.RecipientUserId == recipientUserId,
            cancellationToken);
    }

    public void Add(NotificationEntity notification)
    {
        dbContext.Notifications.Add(notification);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
