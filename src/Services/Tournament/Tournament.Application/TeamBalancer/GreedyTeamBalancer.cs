using Microsoft.Extensions.Options;

namespace Tournament.Application.TeamBalancer;

public sealed class GreedyTeamBalancer : ITeamBalancer
{
    private readonly int maxImprovementIterations;
    private readonly IRandomProvider randomProvider;

    public GreedyTeamBalancer(IRandomProvider randomProvider)
        : this(randomProvider, Options.Create(new TeamBalancingOptions()))
    {
    }

    public GreedyTeamBalancer(
        IRandomProvider randomProvider,
        IOptions<TeamBalancingOptions> options)
    {
        this.randomProvider = randomProvider;
        maxImprovementIterations = Math.Max(0, options.Value.GreedyLocalSearchMaxIterations);
    }

    public IReadOnlyList<BalancedTeam> BuildTeams(
        IReadOnlyCollection<PlayerForBalancing> players,
        int teamSize,
        string disciplineCode,
        Guid tournamentId)
    {
        TeamBalancerBuilder.Validate(players, teamSize);

        var sortedPlayers = players
            .OrderByDescending(player => player.Elo)
            .ThenBy(player => player.Nickname, StringComparer.Ordinal)
            .ThenBy(player => player.PlayerId)
            .ToArray();

        var teamsCount = sortedPlayers.Length / teamSize;
        var teams = Enumerable
            .Range(0, teamsCount)
            .Select(_ => new List<PlayerForBalancing>(teamSize))
            .ToArray();
        var teamTotals = new long[teamsCount];

        foreach (var player in sortedPlayers)
        {
            var teamIndex = Enumerable
                .Range(0, teamsCount)
                .Where(index => teams[index].Count < teamSize)
                .OrderBy(index => teamTotals[index])
                .ThenBy(index => teams[index].Count)
                .ThenBy(index => index)
                .First();

            teams[teamIndex].Add(player);
            teamTotals[teamIndex] += player.Elo;
        }

        ImproveBySwaps(teams, teamTotals, maxImprovementIterations);

        return TeamBalancerBuilder.CreateBalancedTeams(teams, teamSize, randomProvider);
    }

    private static void ImproveBySwaps(
        IReadOnlyList<List<PlayerForBalancing>> teams,
        long[] teamTotals,
        int maxIterations)
    {
        var currentGap = CalculateGap(teamTotals);
        var improved = true;
        var iterations = 0;

        while (improved && iterations < maxIterations)
        {
            improved = false;

            for (var leftTeamIndex = 0; leftTeamIndex < teams.Count && iterations < maxIterations; leftTeamIndex++)
            {
                for (var rightTeamIndex = leftTeamIndex + 1; rightTeamIndex < teams.Count && iterations < maxIterations; rightTeamIndex++)
                {
                    var leftTeam = teams[leftTeamIndex];
                    var rightTeam = teams[rightTeamIndex];

                    for (var leftPlayerIndex = 0; leftPlayerIndex < leftTeam.Count && iterations < maxIterations; leftPlayerIndex++)
                    {
                        for (var rightPlayerIndex = 0; rightPlayerIndex < rightTeam.Count && iterations < maxIterations; rightPlayerIndex++)
                        {
                            iterations++;

                            var leftPlayer = leftTeam[leftPlayerIndex];
                            var rightPlayer = rightTeam[rightPlayerIndex];
                            var eloDelta = rightPlayer.Elo - leftPlayer.Elo;

                            if (eloDelta == 0)
                            {
                                continue;
                            }

                            var leftTotal = teamTotals[leftTeamIndex] + eloDelta;
                            var rightTotal = teamTotals[rightTeamIndex] - eloDelta;
                            var candidateGap = CalculateGap(teamTotals, leftTeamIndex, leftTotal, rightTeamIndex, rightTotal);

                            if (candidateGap >= currentGap)
                            {
                                continue;
                            }

                            leftTeam[leftPlayerIndex] = rightPlayer;
                            rightTeam[rightPlayerIndex] = leftPlayer;
                            teamTotals[leftTeamIndex] = leftTotal;
                            teamTotals[rightTeamIndex] = rightTotal;
                            currentGap = candidateGap;
                            improved = true;
                        }
                    }
                }
            }
        }
    }

    private static long CalculateGap(IReadOnlyList<long> teamTotals)
    {
        return teamTotals.Max() - teamTotals.Min();
    }

    private static long CalculateGap(
        IReadOnlyList<long> teamTotals,
        int firstChangedIndex,
        long firstChangedTotal,
        int secondChangedIndex,
        long secondChangedTotal)
    {
        var min = long.MaxValue;
        var max = long.MinValue;

        for (var index = 0; index < teamTotals.Count; index++)
        {
            var total = index == firstChangedIndex
                ? firstChangedTotal
                : index == secondChangedIndex
                    ? secondChangedTotal
                    : teamTotals[index];

            min = Math.Min(min, total);
            max = Math.Max(max, total);
        }

        return max - min;
    }
}
