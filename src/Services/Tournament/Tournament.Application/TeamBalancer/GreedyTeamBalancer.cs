namespace Tournament.Application.TeamBalancer;

public sealed class GreedyTeamBalancer(IRandomProvider randomProvider) : ITeamBalancer
{
    public IReadOnlyList<BalancedTeam> BuildTeams(
        IReadOnlyCollection<PlayerForBalancing> players,
        int teamSize,
        string disciplineCode,
        Guid tournamentId)
    {
        if (teamSize <= 0)
        {
            throw new TeamBalancingException("Team size must be positive.");
        }

        if (players.Count % teamSize != 0)
        {
            throw new TeamBalancingException("Players count must be divisible by team size.");
        }

        var sortedPlayers = players
            .OrderBy(player => player.Elo)
            .ThenBy(player => player.Nickname, StringComparer.Ordinal)
            .ThenBy(player => player.PlayerId)
            .ToArray();

        var teams = new List<BalancedTeam>();
        for (var i = 0; i < sortedPlayers.Length; i += teamSize)
        {
            var group = sortedPlayers
                .Skip(i)
                .Take(teamSize)
                .ToArray();

            var maxElo = group.Max(player => player.Elo);
            var captainCandidates = group
                .Where(player => player.Elo == maxElo)
                .ToArray();
            var captain = captainCandidates[randomProvider.Next(captainCandidates.Length)];

            var members = group
                .Select(player => new BalancedTeamMember(player.PlayerId, player.Nickname, player.Elo))
                .ToArray();

            teams.Add(new BalancedTeam(
                captain.Nickname,
                captain.PlayerId,
                members,
                group.Average(player => player.Elo)));
        }

        return teams;
    }
}
