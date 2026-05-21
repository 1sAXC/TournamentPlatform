namespace Tournament.Application.TeamBalancer;

internal static class TeamBalancerBuilder
{
    public static void Validate(IReadOnlyCollection<PlayerForBalancing>? players, int teamSize)
    {
        if (players is null)
        {
            throw new TeamBalancingException("Players collection must not be null.");
        }

        if (teamSize <= 0)
        {
            throw new TeamBalancingException("Team size must be positive.");
        }

        if (players.Count == 0)
        {
            throw new TeamBalancingException("Players collection must not be empty.");
        }

        if (players.Count % teamSize != 0)
        {
            throw new TeamBalancingException("Players count must be divisible by team size.");
        }

        if (players.Select(player => player.PlayerId).Distinct().Count() != players.Count)
        {
            throw new TeamBalancingException("Player identifiers must be unique.");
        }

        if (players.Any(player => player.Elo < 0))
        {
            throw new TeamBalancingException("Player ELO must not be negative.");
        }

        if (players.Any(player => string.IsNullOrWhiteSpace(player.Nickname)))
        {
            throw new TeamBalancingException("Player nickname must not be empty.");
        }
    }

    public static BalancedTeam CreateBalancedTeam(
        IReadOnlyCollection<PlayerForBalancing> team,
        IRandomProvider randomProvider)
    {
        var orderedPlayers = team
            .OrderByDescending(player => player.Elo)
            .ThenBy(player => player.Nickname, StringComparer.Ordinal)
            .ThenBy(player => player.PlayerId)
            .ToArray();
        var maxElo = orderedPlayers[0].Elo;
        var captainCandidates = orderedPlayers
            .Where(player => player.Elo == maxElo)
            .ToArray();
        var captain = captainCandidates[randomProvider.Next(captainCandidates.Length)];
        var members = orderedPlayers
            .Select(player => new BalancedTeamMember(player.PlayerId, player.Nickname, player.Elo))
            .ToArray();

        return new BalancedTeam(
            captain.Nickname,
            captain.PlayerId,
            members,
            orderedPlayers.Average(player => player.Elo));
    }

    public static IReadOnlyList<BalancedTeam> CreateBalancedTeams(
        IReadOnlyList<IReadOnlyCollection<PlayerForBalancing>> teams,
        int teamSize,
        IRandomProvider randomProvider)
    {
        EnsureValidTeams(teams, teamSize);

        return teams
            .Select(CreateTeamProjection)
            .OrderBy(team => team.Total)
            .ThenBy(team => team.Index)
            .Select(team => CreateBalancedTeam(team.Team, randomProvider))
            .ToArray();
    }

    public static void EnsureValidTeams(
        IReadOnlyList<IReadOnlyCollection<PlayerForBalancing>> teams,
        int teamSize)
    {
        if (teams.Any(team => team.Count != teamSize))
        {
            throw new TeamBalancingException("Balanced teams must have the requested size.");
        }

        var playerIds = teams
            .SelectMany(team => team.Select(player => player.PlayerId))
            .ToArray();

        if (playerIds.Length != playerIds.Distinct().Count())
        {
            throw new TeamBalancingException("Balanced teams must contain every player only once.");
        }
    }

    private static TeamProjection CreateTeamProjection(
        IReadOnlyCollection<PlayerForBalancing> team,
        int index)
    {
        return new TeamProjection(team, team.Sum(player => (long)player.Elo), index);
    }

    private sealed record TeamProjection(
        IReadOnlyCollection<PlayerForBalancing> Team,
        long Total,
        int Index);
}
