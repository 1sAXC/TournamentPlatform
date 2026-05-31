using Tournament.Application.Brackets;
using Tournament.Application.TeamBalancer;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Tournaments.Services;

public sealed class TournamentLifecycleService(
    IPlayerRatingProjectionRepository ratingProjections,
    ITeamBalancer teamBalancer,
    IBracketGeneratorFactory bracketGeneratorFactory) : ITournamentLifecycleService
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

        tournament.Start(DateTime.UtcNow);
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

        var balancedTeams = teamBalancer.BuildTeams(players, tournament.TeamSize);

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
}
