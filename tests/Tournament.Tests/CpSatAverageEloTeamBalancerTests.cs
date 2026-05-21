using Microsoft.Extensions.Options;
using Tournament.Application.TeamBalancer;

namespace Tournament.Tests;

public sealed class CpSatAverageEloTeamBalancerTests
{
    [Fact]
    public void ShouldMinimizeAverageEloGap_ForSimpleCase()
    {
        var balancer = CreateBalancer();
        var players = Players([1000, 1001, 2000, 2001]);
        const int teamSize = 2;

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        AssertValidBalancedTeams(teams, players, teamSize);
        Assert.Equal(0, CalculateTeamTotalGap(teams));
    }

    [Fact]
    public void ShouldMinimizeAverageEloGap_ForAnotherSimpleCase()
    {
        var balancer = CreateBalancer();
        var players = Players([1000, 1200, 1400, 1600]);
        const int teamSize = 2;

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        AssertValidBalancedTeams(teams, players, teamSize);
        Assert.Equal(0, CalculateTeamTotalGap(teams));
    }

    [Theory]
    [InlineData(new[] { 1000, 1010, 1020, 1600, 1610, 1620 }, 3)]
    [InlineData(new[] { 800, 950, 1100, 1250, 1400, 1550, 1700, 1850 }, 2)]
    [InlineData(new[] { 900, 930, 960, 990, 1300, 1330, 1360, 1390, 1700, 1730 }, 5)]
    public void ShouldMatchBruteForceOptimalGap_ForSmallCases(int[] ratings, int teamSize)
    {
        var balancer = CreateBalancer();
        var players = Players(ratings);
        var expectedGap = CalculateBruteForceOptimalGap(ratings, teamSize);

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        AssertValidBalancedTeams(teams, players, teamSize);
        Assert.Equal(expectedGap, CalculateTeamTotalGap(teams));
    }

    [Fact]
    public void TeamSizeOne_ShouldNotFail()
    {
        var balancer = CreateBalancer();
        var players = Players([1000, 1100, 1200]);
        const int teamSize = 1;

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        Assert.Equal(players.Count, teams.Count);
        AssertValidBalancedTeams(teams, players, teamSize);
    }

    [Fact]
    public void NonDivisiblePlayers_Throws()
    {
        var balancer = CreateBalancer();

        Assert.Throws<TeamBalancingException>(() =>
            balancer.BuildTeams(Players([1000, 1100, 1200]), 2, "CS2", Guid.NewGuid()));
    }

    [Fact]
    public void DuplicatePlayerIds_Throws()
    {
        var balancer = CreateBalancer();
        var playerId = Guid.NewGuid();
        var players = new[]
        {
            new PlayerForBalancing(playerId, "Player1", 1000),
            new PlayerForBalancing(playerId, "Player2", 1100)
        };

        Assert.Throws<TeamBalancingException>(() =>
            balancer.BuildTeams(players, 2, "CS2", Guid.NewGuid()));
    }

    [Fact]
    public void NegativeElo_Throws()
    {
        var balancer = CreateBalancer();
        var players = new[]
        {
            new PlayerForBalancing(Guid.NewGuid(), "Player1", 1000),
            new PlayerForBalancing(Guid.NewGuid(), "Player2", -1)
        };

        Assert.Throws<TeamBalancingException>(() =>
            balancer.BuildTeams(players, 2, "CS2", Guid.NewGuid()));
    }

    private static CpSatAverageEloTeamBalancer CreateBalancer(TeamBalancingOptions? options = null)
    {
        var randomProvider = new DeterministicRandomProvider(0);
        var optionsWrapper = Options.Create(options ?? new TeamBalancingOptions());
        var fallback = new GreedyTeamBalancer(randomProvider, optionsWrapper);

        return new CpSatAverageEloTeamBalancer(fallback, randomProvider, optionsWrapper);
    }

    private static IReadOnlyCollection<PlayerForBalancing> Players(IReadOnlyList<int> ratings)
    {
        return ratings
            .Select((rating, index) => new PlayerForBalancing(
                Guid.NewGuid(),
                $"Player{index + 1}",
                rating))
            .ToArray();
    }

    private static void AssertValidBalancedTeams(
        IReadOnlyCollection<BalancedTeam> teams,
        IReadOnlyCollection<PlayerForBalancing> players,
        int teamSize)
    {
        Assert.All(teams, team => Assert.Equal(teamSize, team.Members.Count));

        var usedPlayerIds = teams
            .SelectMany(team => team.Members.Select(member => member.PlayerId))
            .ToArray();

        Assert.Equal(players.Count, usedPlayerIds.Length);
        Assert.Equal(players.Select(player => player.PlayerId).OrderBy(id => id), usedPlayerIds.OrderBy(id => id));
        Assert.Equal(usedPlayerIds.Length, usedPlayerIds.Distinct().Count());

        foreach (var team in teams)
        {
            var captain = team.Members.Single(member => member.PlayerId == team.CaptainPlayerId);

            Assert.Equal(team.Members.Max(member => member.Elo), captain.Elo);
            Assert.Equal(captain.Nickname, team.Name);
            Assert.Equal(team.Members.Average(member => member.Elo), team.AverageElo);
        }
    }

    private static int CalculateTeamTotalGap(IReadOnlyCollection<BalancedTeam> teams)
    {
        var totals = teams
            .Select(team => team.Members.Sum(member => member.Elo))
            .ToArray();

        return totals.Max() - totals.Min();
    }

    private static int CalculateBruteForceOptimalGap(IReadOnlyList<int> ratings, int teamSize)
    {
        if (ratings.Count > 10)
        {
            throw new ArgumentException("Brute force test helper is intended only for n <= 10.", nameof(ratings));
        }

        var used = new bool[ratings.Count];
        var teamTotals = new List<int>();
        var bestGap = int.MaxValue;

        Search();
        return bestGap;

        void Search()
        {
            var firstUnused = Array.FindIndex(used, value => !value);
            if (firstUnused < 0)
            {
                var gap = teamTotals.Max() - teamTotals.Min();
                bestGap = Math.Min(bestGap, gap);
                return;
            }

            used[firstUnused] = true;
            var currentTeam = new List<int> { firstUnused };
            BuildTeam(firstUnused + 1, currentTeam, ratings[firstUnused]);
            used[firstUnused] = false;
        }

        void BuildTeam(int startIndex, List<int> currentTeam, int currentTotal)
        {
            if (currentTeam.Count == teamSize)
            {
                teamTotals.Add(currentTotal);
                Search();
                teamTotals.RemoveAt(teamTotals.Count - 1);
                return;
            }

            for (var index = startIndex; index < ratings.Count; index++)
            {
                if (used[index])
                {
                    continue;
                }

                used[index] = true;
                currentTeam.Add(index);
                BuildTeam(index + 1, currentTeam, currentTotal + ratings[index]);
                currentTeam.RemoveAt(currentTeam.Count - 1);
                used[index] = false;
            }
        }
    }

    private sealed class DeterministicRandomProvider(int value) : IRandomProvider
    {
        public int Next(int maxExclusive)
        {
            return value % maxExclusive;
        }
    }
}
