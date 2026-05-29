using Tournament.Application.Tournaments.Abstractions;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Tournament.Application.Brackets;

public abstract class BracketGeneratorBase(IOutboxWriter outboxWriter) : IBracketGenerator
{
    public virtual Task GenerateInitialAsync(
        Domain.Tournaments.Tournament tournament,
        IReadOnlyList<Team> teams,
        CancellationToken ct)
    {
        if (tournament.Rounds.Count > 0)
        {
            return Task.CompletedTask;
        }

        AddInitialStandings(tournament, teams);
        CreateRound(tournament, 1, BracketType, SeedForInitialRound(teams));
        return Task.CompletedTask;
    }

    public virtual Task HandleMatchCompletedAsync(
        Domain.Tournaments.Tournament tournament,
        Match completedMatch,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    protected abstract BracketType BracketType { get; }

    protected virtual void AddInitialStandings(Domain.Tournaments.Tournament tournament, IReadOnlyList<Team> teams)
    {
    }

    protected static IReadOnlyList<Team> SeedForInitialRound(IReadOnlyList<Team> teams)
    {
        return teams
            .OrderByDescending(team => team.AverageElo)
            .ThenBy(team => team.Id)
            .ToArray();
    }

    protected Round CreateRound(
        Domain.Tournaments.Tournament tournament,
        int roundNumber,
        BracketType bracketType,
        IReadOnlyList<Team> orderedTeams)
    {
        var now = DateTime.UtcNow;
        var round = Round.Create(tournament.Id, roundNumber, bracketType, now);
        round.Start();

        var matchNumber = 1;
        var left = 0;
        var right = orderedTeams.Count - 1;
        while (left <= right)
        {
            var teamA = orderedTeams[left++];
            var teamB = left <= right ? orderedTeams[right--] : null;
            var match = Match.Create(tournament.Id, matchNumber++, teamA.Id, teamB?.Id, now);
            if (teamB is null)
            {
                match.CompleteBye(teamA.Id, now);
            }

            round.AddMatch(match);
        }

        tournament.AddRound(round);
        tournament.AdvanceToRound(roundNumber);
        EmitRoundCreatedEvent(tournament, round);
        return round;
    }

    // Called after a new round has been added to the tournament aggregate.
    // Publishes a RoundCreatedEvent carrying enough data for downstream
    // consumers (notifications) to fan out per-match without any extra
    // calls back into Tournament.Api.
    protected void EmitRoundCreatedEvent(Domain.Tournaments.Tournament tournament, Round round)
    {
        var teamIds = round.Matches
            .SelectMany(match => new[] { match.TeamAId, match.TeamBId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToHashSet();

        var teams = tournament.Teams
            .Where(team => teamIds.Contains(team.Id))
            .OrderBy(team => team.Seed)
            .Select(team => new EventTeamDto
            {
                TeamId = team.Id,
                Name = team.Name,
                CaptainUserId = team.CaptainPlayerId,
                Members = team.Members.Select(member => new EventTeamMemberDto
                {
                    UserId = member.PlayerId,
                    Nickname = member.Nickname,
                    Elo = member.Elo,
                    IsCaptain = member.PlayerId == team.CaptainPlayerId
                }).ToArray()
            })
            .ToArray();

        var matches = round.Matches
            .OrderBy(match => match.MatchNumber)
            .Select(match => new EventMatchDto
            {
                MatchId = match.Id,
                MatchNumber = match.MatchNumber,
                TeamAId = match.TeamAId,
                TeamBId = match.TeamBId
            })
            .ToArray();

        outboxWriter.Add(new RoundCreatedEvent
        {
            TournamentId = tournament.Id,
            TournamentTitle = tournament.Title,
            DisciplineCode = tournament.DisciplineCode,
            OrganizerId = tournament.OrganizerId,
            RoundId = round.Id,
            RoundNumber = round.Number,
            BracketType = round.BracketType.ToString(),
            Teams = teams,
            Matches = matches
        });
    }

    protected void CompleteTournament(
        Domain.Tournaments.Tournament tournament,
        IReadOnlyList<Guid> orderedTeamIds)
    {
        if (tournament.Status == TournamentStatus.Completed)
        {
            return;
        }

        var now = DateTime.UtcNow;
        tournament.Complete(now);
        outboxWriter.Add(new TournamentCompletedEvent
        {
            TournamentId = tournament.Id,
            TournamentName = tournament.Title,
            DisciplineCode = tournament.DisciplineCode,
            CompletedAtUtc = now,
            Standings = orderedTeamIds
                .Select((teamId, index) =>
                {
                    var team = tournament.Teams.Single(t => t.Id == teamId);
                    return new TournamentStandingDto
                    {
                        TeamId = team.Id,
                        TeamName = team.Name,
                        Place = index + 1,
                        Members = team.Members.Select(member => new EventTeamMemberDto
                        {
                            UserId = member.PlayerId,
                            Nickname = member.Nickname,
                            Elo = member.Elo,
                            IsCaptain = member.PlayerId == team.CaptainPlayerId
                        }).ToArray()
                    };
                })
                .ToArray()
        });
    }

    protected static Round? FindRound(Domain.Tournaments.Tournament tournament, Match match)
    {
        return tournament.Rounds.SingleOrDefault(round => round.Matches.Any(m => m.Id == match.Id));
    }
}
