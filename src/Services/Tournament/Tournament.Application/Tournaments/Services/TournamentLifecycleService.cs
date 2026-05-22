using Tournament.Application.Brackets;
using Tournament.Application.TeamBalancer;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Tournaments.Services;

public sealed class TournamentLifecycleService(
    IPlayerRatingProjectionRepository ratingProjections,
    ITeamBalancer teamBalancer,
    IBracketGeneratorFactory bracketGeneratorFactory,
    IOutboxWriter outboxWriter) : ITournamentLifecycleService
{
    private const int DefaultElo = 1000;

    public async Task TryStartTournamentAsync(
        Domain.Tournaments.Tournament tournament,
        CancellationToken cancellationToken = default)
    {
        if (tournament.Status != TournamentStatus.Open)
        {
            return;
        }

        if (tournament.ActiveParticipantsCount != tournament.MaxPlayers)
        {
            return;
        }

        tournament.MarkFull();

        var activeParticipants = tournament.Participants
            .Where(participant => participant.IsActive)
            .OrderBy(participant => participant.RegisteredAtUtc)
            .ToArray();

        var teams = await EnsureTeamsAsync(tournament, activeParticipants, cancellationToken);

        if (tournament.Rounds.Count == 0)
        {
            var generator = bracketGeneratorFactory.GetGenerator(tournament.Format);
            await generator.GenerateInitialAsync(tournament, teams, cancellationToken);
        }

        var startedAtUtc = DateTime.UtcNow;
        tournament.Start(startedAtUtc);
        outboxWriter.Add(ToTournamentStartedEvent(tournament, startedAtUtc));
    }

    private async Task<IReadOnlyList<Team>> EnsureTeamsAsync(
        Domain.Tournaments.Tournament tournament,
        IReadOnlyCollection<TournamentParticipant> activeParticipants,
        CancellationToken cancellationToken)
    {
        if (tournament.Teams.Count > 0)
        {
            return tournament.Teams.OrderBy(team => team.Seed).ToArray();
        }

        var playerIds = activeParticipants
            .Select(participant => participant.PlayerId)
            .ToArray();
        var ratings = await ratingProjections.GetByPlayerIdsAsync(
            playerIds,
            tournament.DisciplineCode,
            cancellationToken);
        var ratingsByPlayer = ratings.ToDictionary(rating => rating.PlayerId);
        var now = DateTime.UtcNow;

        var players = activeParticipants.Select(participant =>
        {
            if (!ratingsByPlayer.TryGetValue(participant.PlayerId, out var rating))
            {
                rating = PlayerRatingProjection.Create(
                    participant.PlayerId,
                    tournament.DisciplineCode,
                    DefaultElo,
                    now);

                ratingProjections.Add(rating);
                ratingsByPlayer[participant.PlayerId] = rating;
            }

            return new PlayerForBalancing(participant.PlayerId, participant.PlayerNickname, rating.Elo);
        }).ToArray();

        var balancedTeams = teamBalancer.BuildTeams(
            players,
            tournament.TeamSize,
            tournament.DisciplineCode,
            tournament.Id);

        var teams = balancedTeams.Select((team, index) => Team.Create(
            tournament.Id,
            team.Name,
            team.CaptainPlayerId,
            index + 1,
            team.AverageElo,
            team.Members.Select(member => TeamMember.Create(
                member.PlayerId,
                member.Nickname,
                member.Elo)))).ToArray();

        tournament.AddTeams(teams);
        return teams;
    }

    private static TournamentStartedEvent ToTournamentStartedEvent(
        Domain.Tournaments.Tournament tournament,
        DateTime startedAtUtc)
    {
        return new TournamentStartedEvent
        {
            TournamentId = tournament.Id,
            OrganizerId = tournament.OrganizerId,
            TournamentName = tournament.Title,
            DisciplineCode = tournament.DisciplineCode,
            Format = tournament.Format.ToString(),
            TournamentFormat = tournament.Format.ToString(),
            TeamSize = tournament.TeamSize,
            StartedAtUtc = startedAtUtc,
            Teams = tournament.Teams
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
                }).ToArray(),
            Rounds = tournament.Rounds
                .OrderBy(round => round.Number)
                .Select(round => new EventRoundDto
                {
                    RoundId = round.Id,
                    Number = round.Number,
                    BracketType = round.BracketType.ToString(),
                    Matches = round.Matches
                        .OrderBy(match => match.MatchNumber)
                        .Select(match => new EventMatchDto
                        {
                            MatchId = match.Id,
                            MatchNumber = match.MatchNumber,
                            TeamAId = match.TeamAId,
                            TeamBId = match.TeamBId
                        })
                        .ToArray()
                })
                .ToArray()
        };
    }
}
