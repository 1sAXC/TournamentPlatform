using TournamentPlatform.Contracts.Events;

namespace Notification.Application.Notifications.Services;

/// <summary>
/// Fans out a RoundCreatedEvent into per-recipient Notification rows —
/// one per member of each team that has a match in the new round.
/// Lives in Application so it can be unit-tested with an in-memory
/// repository, without standing up RabbitMQ.
/// </summary>
public interface IRoundCreatedFanout
{
    Task<int> FanoutAsync(RoundCreatedEvent integrationEvent, CancellationToken cancellationToken = default);
}
