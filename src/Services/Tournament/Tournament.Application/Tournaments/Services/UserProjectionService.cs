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
                integrationEvent.ContactHandle,
                integrationEvent.OrganizerName,
                integrationEvent.CreatedAtUtc));
        }
        else
        {
            projection.Restore(integrationEvent.Role, integrationEvent.OrganizerName);
            projection.UpdateContactHandle(integrationEvent.ContactHandle);
        }

        await users.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleUserBlockedAsync(
        UserBlockedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var projection = await users.GetByIdAsync(integrationEvent.UserId, cancellationToken);
        if (projection is not null)
        {
            projection.MarkBlocked(integrationEvent.BlockedAtUtc);
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
            // Late-arriving role change without a prior UserCreated — record the
            // user but leave contact handle null; a subsequent ContactHandleChanged
            // (or a re-emitted UserCreated) will fill it in.
            users.Add(UserProjection.Create(
                integrationEvent.UserId,
                integrationEvent.NewRole,
                contactHandle: null,
                integrationEvent.OrganizerName,
                integrationEvent.ChangedAtUtc));
        }
        else
        {
            projection.ChangeRole(integrationEvent.NewRole, integrationEvent.OrganizerName);
        }

        await users.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleUserContactHandleChangedAsync(
        UserContactHandleChangedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var projection = await users.GetByIdAsync(integrationEvent.UserId, cancellationToken);
        if (projection is null)
        {
            // Out-of-order delivery — projection has not been created yet. Skip;
            // when UserCreated arrives it carries the latest handle anyway.
            return;
        }

        projection.UpdateContactHandle(integrationEvent.ContactHandle);
        await users.SaveChangesAsync(cancellationToken);
    }
}
