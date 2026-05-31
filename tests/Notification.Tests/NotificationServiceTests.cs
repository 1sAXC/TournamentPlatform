using Notification.Application.Notifications;
using Notification.Application.Notifications.Abstractions;
using Notification.Application.Notifications.Services;
using Notification.Domain.Notifications;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Tests;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task List_ReturnsOnlyOwnNotifications_WithCorrectCounters()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var repository = new InMemoryRepository();
        repository.Notifications.AddRange([
            CreateNotification(ownerId, isRead: false),
            CreateNotification(ownerId, isRead: false),
            CreateNotification(ownerId, isRead: true),
            CreateNotification(otherId, isRead: false),
        ]);
        var service = new NotificationService(repository);

        var page = await service.ListAsync(ownerId, pageNumber: 1, pageSize: 20);

        Assert.Equal(3, page.Items.Count);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.UnreadCount);
    }

    [Fact]
    public async Task MarkRead_OwnNotification_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var notification = CreateNotification(ownerId, isRead: false);
        var repository = new InMemoryRepository();
        repository.Notifications.Add(notification);
        var service = new NotificationService(repository);

        var result = await service.MarkReadAsync(notification.Id, ownerId);

        Assert.True(result.IsSuccess);
        Assert.True(notification.IsRead);
    }

    [Fact]
    public async Task MarkRead_OtherUsersNotification_ReturnsAccessDenied()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var notification = CreateNotification(ownerId, isRead: false);
        var repository = new InMemoryRepository();
        repository.Notifications.Add(notification);
        var service = new NotificationService(repository);

        var result = await service.MarkReadAsync(notification.Id, otherId);

        Assert.True(result.IsFailure);
        Assert.Equal(NotificationErrors.AccessDenied, result.Error);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task MarkRead_NonExisting_ReturnsNotFound()
    {
        var repository = new InMemoryRepository();
        var service = new NotificationService(repository);

        var result = await service.MarkReadAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(NotificationErrors.NotFound, result.Error);
    }

    private static NotificationEntity CreateNotification(Guid recipientId, bool isRead)
    {
        var n = NotificationEntity.Create(
            recipientId,
            NotificationType.MatchCreated,
            "Title",
            "Body",
            "/tournaments/x/matches/y",
            "{}",
            sourceEventId: Guid.NewGuid(),
            createdAtUtc: DateTime.UtcNow);

        if (isRead)
        {
            n.MarkRead(DateTime.UtcNow);
        }

        return n;
    }

    private sealed class InMemoryRepository : INotificationRepository
    {
        public List<NotificationEntity> Notifications { get; } = [];

        public Task<IReadOnlyCollection<NotificationEntity>> GetForUserAsync(Guid recipientUserId, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<NotificationEntity>>(
                Notifications
                    .Where(n => n.RecipientUserId == recipientUserId)
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .Skip(skip)
                    .Take(take)
                    .ToArray());

        public Task<int> CountForUserAsync(Guid recipientUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(n => n.RecipientUserId == recipientUserId));

        public Task<int> CountUnreadForUserAsync(Guid recipientUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(n => n.RecipientUserId == recipientUserId && !n.IsRead));

        public Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.FirstOrDefault(n => n.Id == id));

        public Task<bool> ExistsForSourceEventAsync(Guid sourceEventId, Guid recipientUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Any(n => n.SourceEventId == sourceEventId && n.RecipientUserId == recipientUserId));

        public void Add(NotificationEntity notification) => Notifications.Add(notification);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
