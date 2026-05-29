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
        if (!string.Equals(integrationEvent.Role, "Player", StringComparison.OrdinalIgnoreCase))
        {
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

    public async Task HandleUserDeletedAsync(
        UserDeletedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await projections.DeletedUserExistsAsync(integrationEvent.UserId, cancellationToken))
        {
            await projections.AddDeletedUserAsync(
                integrationEvent.UserId,
                integrationEvent.DeletedAtUtc,
                cancellationToken);
        }

        await projections.SaveChangesAsync(cancellationToken);
    }
}
