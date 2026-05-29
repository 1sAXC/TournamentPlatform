using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Services;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Shared.Common;

namespace Tournament.Application.Brackets;

public sealed class SwissRoundService(
    ITournamentRepository tournaments,
    IBracketGeneratorFactory bracketGeneratorFactory) : ISwissRoundService
{
    public async Task<Result> CreateNextRoundAsync(
        Guid tournamentId,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result.Failure(TournamentErrors.TournamentNotFound);
        }

        var allowed = string.Equals(currentUser.Role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase)
            || (string.Equals(currentUser.Role, UserRole.Organizer.ToString(), StringComparison.OrdinalIgnoreCase)
                && tournament.OrganizerId == currentUser.Id);
        if (!allowed)
        {
            return Result.Failure(TournamentErrors.AccessDenied);
        }

        if (tournament.Status != TournamentStatus.InProgress || tournament.Format != TournamentFormat.Swiss)
        {
            return Result.Failure(TournamentErrors.TournamentAlreadyStarted);
        }

        var currentRound = tournament.Rounds
            .Where(round => round.BracketType == BracketType.Swiss)
            .OrderByDescending(round => round.Number)
            .FirstOrDefault();
        if (currentRound is null || currentRound.Status != RoundStatus.Completed)
        {
            return Result.Failure(TournamentErrors.CurrentRoundNotCompleted);
        }

        if (currentRound.Number >= tournament.SwissRounds)
        {
            return Result.Failure(TournamentErrors.TournamentAlreadyStarted);
        }

        var generator = (SwissBracketGenerator)bracketGeneratorFactory.GetGenerator(TournamentFormat.Swiss);
        generator.CreateNextRound(tournament);
        await tournaments.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
