using Tournament.Application.Tournaments.Abstractions;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Common;
using TournamentPlatform.Contracts.Events;

namespace Tournament.Application.Tournaments.Services;

public sealed class PlayerRatingProjectionService(IPlayerRatingProjectionRepository projections)
    : IPlayerRatingProjectionService
{
    private const int InitialElo = 1000;

    private static readonly IReadOnlyCollection<string> InitialDisciplines =
    [
        DisciplineCodes.CS2,
        DisciplineCodes.Valorant,
        DisciplineCodes.Standoff2
    ];

    public async Task HandleUserCreatedAsync(
        UserCreatedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        // Unblocking re-emits UserCreatedEvent (see User.Unblock). Clear any
        // existing block projection so the user can register for tournaments
        // again. Role-agnostic on purpose because the block-side
        // (HandleUserBlockedAsync) is also role-agnostic.
        await projections.RemoveBlockedUserAsync(integrationEvent.UserId, cancellationToken);

        if (!string.Equals(integrationEvent.Role, "Player", StringComparison.OrdinalIgnoreCase))
        {
            await projections.SaveChangesAsync(cancellationToken);
            return;
        }

        var existing = await projections.GetByPlayerIdAsync(integrationEvent.UserId, cancellationToken);
        var existingCodes = existing
            .Select(projection => projection.DisciplineCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var disciplineCode in InitialDisciplines)
        {
            if (existingCodes.Contains(disciplineCode))
            {
                continue;
            }

            projections.Add(PlayerRatingProjection.Create(
                integrationEvent.UserId,
                disciplineCode,
                InitialElo,
                integrationEvent.CreatedAtUtc));
        }

        await projections.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleRatingUpdatedAsync(
        RatingUpdatedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var disciplineCode = integrationEvent.DisciplineCode.Trim();
        var projection = await projections.GetAsync(
            integrationEvent.UserId,
            disciplineCode,
            cancellationToken);

        if (projection is null)
        {
            projections.Add(PlayerRatingProjection.Create(
                integrationEvent.UserId,
                disciplineCode,
                integrationEvent.NewElo,
                integrationEvent.UpdatedAtUtc));
        }
        else if (integrationEvent.UpdatedAtUtc >= projection.UpdatedAtUtc)
        {
            projection.UpdateElo(integrationEvent.NewElo, integrationEvent.UpdatedAtUtc);
        }

        await projections.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleUserBlockedAsync(
        UserBlockedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await projections.BlockedUserExistsAsync(integrationEvent.UserId, cancellationToken))
        {
            await projections.AddBlockedUserAsync(
                integrationEvent.UserId,
                integrationEvent.BlockedAtUtc,
                cancellationToken);
        }

        await projections.SaveChangesAsync(cancellationToken);
    }
}
