using Microsoft.EntityFrameworkCore;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Exceptions;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Infrastructure.Persistence.Repositories;

public sealed class TournamentRepository(TournamentDbContext dbContext) : ITournamentRepository
{
    public Task<bool> TitleExistsAsync(string normalizedTitle, CancellationToken cancellationToken = default)
    {
        return dbContext.Tournaments.AnyAsync(
            tournament => tournament.NormalizedTitle == normalizedTitle,
            cancellationToken);
    }

    public Task<Discipline?> GetActiveDisciplineAsync(
        string disciplineCode,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Disciplines.FirstOrDefaultAsync(
            discipline => discipline.Code == disciplineCode && discipline.IsActive,
            cancellationToken);
    }

    public Task<Domain.Tournaments.Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return IncludeParticipants(dbContext.Tournaments)
            .FirstOrDefaultAsync(
                tournament => tournament.Id == id && !tournament.IsDeleted,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await IncludeParticipants(dbContext.Tournaments)
            .Where(tournament => !tournament.IsDeleted)
            .OrderByDescending(tournament => tournament.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetByStatusAsync(
        TournamentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await IncludeParticipants(dbContext.Tournaments)
            .Where(tournament => !tournament.IsDeleted && tournament.Status == status)
            .OrderByDescending(tournament => tournament.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        return await IncludeParticipants(dbContext.Tournaments)
            .Where(tournament => !tournament.IsDeleted
                && tournament.Status == TournamentStatus.Open
                && tournament.Participants.Count(participant => participant.IsActive) < tournament.MaxPlayers)
            .OrderByDescending(tournament => tournament.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetByOrganizerAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default)
    {
        return await IncludeParticipants(dbContext.Tournaments)
            .Where(tournament => !tournament.IsDeleted && tournament.OrganizerId == organizerId)
            .OrderByDescending(tournament => tournament.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetByPlayerAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        return await IncludeParticipants(dbContext.Tournaments)
            .Where(tournament => !tournament.IsDeleted
                && tournament.Participants.Any(participant => participant.PlayerId == playerId
                    && (participant.IsActive || tournament.StartedAtUtc != null)))
            .OrderByDescending(tournament => tournament.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> DeletedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.DeletedUserProjections.AnyAsync(
            projection => projection.UserId == userId,
            cancellationToken);
    }

    public void Add(Domain.Tournaments.Tournament tournament)
    {
        dbContext.Tournaments.Add(tournament);
    }

    public async Task<ITournamentTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new TournamentEfTransaction(transaction);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new TournamentPersistenceConflictException("Tournament was modified concurrently.", exception);
        }
        catch (DbUpdateException exception)
        {
            throw new TournamentPersistenceConflictException("Tournament persistence conflict.", exception);
        }
    }

    private static IQueryable<Domain.Tournaments.Tournament> IncludeParticipants(
        IQueryable<Domain.Tournaments.Tournament> query)
    {
        return query
            .Include(tournament => tournament.Participants)
            .Include(tournament => tournament.Teams)
            .ThenInclude(team => team.Members)
            .Include(tournament => tournament.Rounds)
            .ThenInclude(round => round.Matches)
            .Include(tournament => tournament.SwissStandings)
            .Include(tournament => tournament.DoubleEliminationStandings);
    }
}
