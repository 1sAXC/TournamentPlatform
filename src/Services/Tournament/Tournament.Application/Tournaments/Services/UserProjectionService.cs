using Tournament.Application.Tournaments.Abstractions;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Events;

namespace Tournament.Application.Tournaments.Services;

public sealed class UserProjectionService(IUserProjectionRepository users) : IUserProjectionService
{
    public async Task HandleUserCreatedAsync(
        UserCreatedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var projection = await users.GetByIdAsync(integrationEvent.UserId, cancellationToken);
        if (projection is null)
        {
            users.Add(UserProjection.Create(
                integrationEvent.UserId,
                integrationEvent.Role,
                integrationEvent.CreatedAtUtc));
        }
        else
        {
            projection.Restore(integrationEvent.Role);
        }

        await users.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleUserDeletedAsync(
        UserDeletedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var projection = await users.GetByIdAsync(integrationEvent.UserId, cancellationToken);
        if (projection is not null)
        {
            projection.MarkDeleted(integrationEvent.DeletedAtUtc);
            await users.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleUserRoleChangedAsync(
        UserRoleChangedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var projection = await users.GetByIdAsync(integrationEvent.UserId, cancellationToken);
        if (projection is null)
        {
            users.Add(UserProjection.Create(
                integrationEvent.UserId,
                integrationEvent.NewRole,
                integrationEvent.ChangedAtUtc));
        }
        else
        {
            projection.ChangeRole(integrationEvent.NewRole);
        }

        await users.SaveChangesAsync(cancellationToken);
    }
}
