using Tournament.Application.Brackets;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Dto;
using Tournament.Application.Tournaments.Services;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Shared.Common;

namespace Tournament.Application.Matches;

public sealed class MatchResultService(
    ITournamentRepository tournaments,
    IBracketGeneratorFactory bracketGeneratorFactory,
    IOutboxWriter outboxWriter) : IMatchResultService
{
    public async Task<Result<TournamentDetailsResponse>> CompleteMatchAsync(
        Guid tournamentId,
        Guid matchId,
        CompleteMatchRequest request,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentNotFound);
        }

        if (!CanComplete(tournament, currentUser))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.AccessDenied);
        }

        var match = tournament.Rounds
            .SelectMany(round => round.Matches)
            .SingleOrDefault(match => match.Id == matchId);
        if (match is null || match.TournamentId != tournamentId)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.ParticipantNotFound);
        }

        if (match.Status == MatchStatus.Completed)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.MatchAlreadyCompleted);
        }

        if (tournament.Status != TournamentStatus.InProgress)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentNotInProgress);
        }

        if (match.TeamAId is null || match.TeamBId is null)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentRegistrationClosed);
        }

        if (request.WinnerTeamId != match.TeamAId && request.WinnerTeamId != match.TeamBId)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.InvalidWinnerTeam);
        }

        if (!request.IsTechnicalDefeat
            && (request.WinnerScore is null || request.LoserScore is null
                || request.WinnerMaps is null || request.LoserMaps is null))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.MatchScoreRequired);
        }

        if (request.WinnerScore is not null
            && request.LoserScore is not null
            && request.WinnerScore <= request.LoserScore)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.InvalidMatchScore);
        }

        if (request.WinnerMaps is not null
            && request.LoserMaps is not null
            && request.WinnerMaps <= request.LoserMaps)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.InvalidMatchScore);
        }

        match.Complete(
            request.WinnerTeamId,
            request.WinnerScore,
            request.LoserScore,
            request.WinnerMaps,
            request.LoserMaps,
            request.IsTechnicalDefeat,
            DateTime.UtcNow);

        outboxWriter.Add(ToMatchCompletedEvent(tournament, match));

        var generator = bracketGeneratorFactory.GetGenerator(tournament.Format);
        await generator.HandleMatchCompletedAsync(tournament, match, cancellationToken);
        await tournaments.SaveChangesAsync(cancellationToken);

        return Result<TournamentDetailsResponse>.Success(TournamentResponseMapper.ToDetailsResponse(tournament));
    }

    private static bool CanComplete(Domain.Tournaments.Tournament tournament, CurrentTournamentUser currentUser)
    {
        return string.Equals(currentUser.Role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase)
            || (string.Equals(currentUser.Role, UserRole.Organizer.ToString(), StringComparison.OrdinalIgnoreCase)
                && tournament.OrganizerId == currentUser.Id);
    }

    private static MatchCompletedEvent ToMatchCompletedEvent(
        Domain.Tournaments.Tournament tournament,
        Domain.Tournaments.Match match)
    {
        var winner = tournament.Teams.Single(team => team.Id == match.WinnerTeamId);
        var loser = tournament.Teams.Single(team => team.Id == match.LoserTeamId);
        return new MatchCompletedEvent
        {
            MatchId = match.Id,
            TournamentId = tournament.Id,
            DisciplineCode = tournament.DisciplineCode,
            TeamSize = tournament.TeamSize,
            WinnerTeamId = winner.Id,
            LoserTeamId = loser.Id,
            WinnerScore = match.WinnerScore,
            LoserScore = match.LoserScore,
            WinnerMaps = match.WinnerMaps,
            LoserMaps = match.LoserMaps,
            IsTechnicalDefeat = match.IsTechnicalDefeat,
            WinnerPlayers = winner.Members.Select(member => new MatchCompletedPlayerDto
            {
                UserId = member.PlayerId
            }).ToArray(),
            LoserPlayers = loser.Members.Select(member => new MatchCompletedPlayerDto
            {
                UserId = member.PlayerId
            }).ToArray()
        };
    }
}
