using TournamentPlatform.Contracts.Events;

namespace Tournament.Application.Tournaments.Services;

public interface IUserProjectionService
{
    Task HandleUserCreatedAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default);
    Task HandleUserBlockedAsync(UserBlockedEvent integrationEvent, CancellationToken cancellationToken = default);
    Task HandleUserRoleChangedAsync(UserRoleChangedEvent integrationEvent, CancellationToken cancellationToken = default);
    Task HandleUserContactHandleChangedAsync(UserContactHandleChangedEvent integrationEvent, CancellationToken cancellationToken = default);
}
