using System.Diagnostics;
using Microsoft.Extensions.Options;
using Tournament.Application.TeamBalancer;
using Xunit.Abstractions;

namespace Tournament.Tests;

public sealed class CpSatAverageEloTeamBalancerTests(ITestOutputHelper output)
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

    [Fact]
    public void LargeCase_ShouldCompleteWithinSolverTimeLimitAndReturnValidTeams()
    {
        var balancer = CreateBalancer(new TeamBalancingOptions { CpSatTimeLimitSeconds = 2 });
        var ratings = Enumerable
            .Range(0, 60)
            .Select(index => 800 + index * 17 % 1300)
            .ToArray();
        var players = Players(ratings);
        const int teamSize = 5;
        var stopwatch = Stopwatch.StartNew();

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        stopwatch.Stop();
        WriteElapsed("60 players, team size 5", stopwatch.Elapsed, balancer.LastDiagnostics);
        AssertValidBalancedTeams(teams, players, teamSize);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(15), $"Elapsed: {stopwatch.Elapsed}");
    }

    [Fact]
    public void ComparisonCase_100PlayersTeamSize5_ShouldCompareCpSatWithFallback()
    {
        var options = Options.Create(new TeamBalancingOptions { CpSatTimeLimitSeconds = 60 });
        var randomProvider = new DeterministicRandomProvider(0);
        var fallback = new GreedyTeamBalancer(randomProvider, options);
        var cpSat = new CpSatAverageEloTeamBalancer(fallback, randomProvider, options);
        var ratings = Enumerable
            .Range(0, 100)
            .Select(index => 750 + index * 41 % 1700)
            .ToArray();
        var players = Players(ratings);
        const int teamSize = 5;

        var fallbackStopwatch = Stopwatch.StartNew();
        var fallbackTeams = fallback.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        fallbackStopwatch.Stop();

        var cpSatStopwatch = Stopwatch.StartNew();
        var cpSatTeams = cpSat.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        cpSatStopwatch.Stop();

        var fallbackGap = CalculateTeamTotalGap(fallbackTeams);
        var cpSatGap = CalculateTeamTotalGap(cpSatTeams);

        WriteComparison(
            "100 players, team size 5",
            fallbackStopwatch.Elapsed,
            fallbackGap,
            cpSatStopwatch.Elapsed,
            cpSatGap,
            cpSat.LastDiagnostics);
        AssertValidBalancedTeams(fallbackTeams, players, teamSize);
        AssertValidBalancedTeams(cpSatTeams, players, teamSize);
        Assert.True(cpSatGap <= fallbackGap, $"CP-SAT gap {cpSatGap} should be <= fallback gap {fallbackGap}.");
    }

    [Fact]
    public void ComparisonCase_100PlayersTeamSize5_ShouldComparePlainCpSatWithHintedCpSat()
    {
        var options = Options.Create(new TeamBalancingOptions { CpSatTimeLimitSeconds = 60 });
        var randomProvider = new DeterministicRandomProvider(0);
        var fallback = new GreedyTeamBalancer(randomProvider, options);
        var plainCpSat = new CpSatAverageEloTeamBalancer(fallback, randomProvider, options);
        var hintedCpSat = new HintedCpSatAverageEloTeamBalancer(fallback, randomProvider, options);
        var ratings = Enumerable
            .Range(0, 100)
            .Select(index => 750 + index * 41 % 1700)
            .ToArray();
        var players = Players(ratings);
        const int teamSize = 5;

        var fallbackStopwatch = Stopwatch.StartNew();
        var fallbackTeams = fallback.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        fallbackStopwatch.Stop();

        var plainStopwatch = Stopwatch.StartNew();
        var plainTeams = plainCpSat.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        plainStopwatch.Stop();

        var hintedStopwatch = Stopwatch.StartNew();
        var hintedTeams = hintedCpSat.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        hintedStopwatch.Stop();

        WriteHintComparison(
            "100 players, team size 5",
            fallbackStopwatch.Elapsed,
            CalculateTeamTotalGap(fallbackTeams),
            plainStopwatch.Elapsed,
            CalculateTeamTotalGap(plainTeams),
            plainCpSat.LastDiagnostics,
            hintedStopwatch.Elapsed,
            CalculateTeamTotalGap(hintedTeams),
            hintedCpSat.LastDiagnostics);
        AssertValidBalancedTeams(fallbackTeams, players, teamSize);
        AssertValidBalancedTeams(plainTeams, players, teamSize);
        AssertValidBalancedTeams(hintedTeams, players, teamSize);
    }

    [Theory]
    [InlineData(100, 60)]
    [InlineData(1000, 20)]
    public void ComparisonCase_ShouldComparePlainCpSatWithUpperBoundCpSat(int playersCount, int cpSatTimeLimitSeconds)
    {
        var options = Options.Create(new TeamBalancingOptions { CpSatTimeLimitSeconds = cpSatTimeLimitSeconds });
        var randomProvider = new DeterministicRandomProvider(0);
        var fallback = new GreedyTeamBalancer(randomProvider, options);
        var plainCpSat = new CpSatAverageEloTeamBalancer(fallback, randomProvider, options);
        var upperBoundCpSat = new UpperBoundCpSatAverageEloTeamBalancer(fallback, randomProvider, options);
        var ratings = Enumerable
            .Range(0, playersCount)
            .Select(index => playersCount == 100
                ? 750 + index * 41 % 1700
                : 600 + index * 53 % 2400)
            .ToArray();
        var players = Players(ratings);
        const int teamSize = 5;

        var fallbackStopwatch = Stopwatch.StartNew();
        var fallbackTeams = fallback.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        fallbackStopwatch.Stop();

        var plainStopwatch = Stopwatch.StartNew();
        var plainTeams = plainCpSat.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        plainStopwatch.Stop();

        var upperBoundStopwatch = Stopwatch.StartNew();
        var upperBoundTeams = upperBoundCpSat.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        upperBoundStopwatch.Stop();

        WriteUpperBoundComparison(
            $"{playersCount} players, team size 5",
            fallbackStopwatch.Elapsed,
            CalculateTeamTotalGap(fallbackTeams),
            plainStopwatch.Elapsed,
            CalculateTeamTotalGap(plainTeams),
            plainCpSat.LastDiagnostics,
            upperBoundStopwatch.Elapsed,
            CalculateTeamTotalGap(upperBoundTeams),
            upperBoundCpSat.LastDiagnostics);
        AssertValidBalancedTeams(fallbackTeams, players, teamSize);
        AssertValidBalancedTeams(plainTeams, players, teamSize);
        AssertValidBalancedTeams(upperBoundTeams, players, teamSize);
    }

    [Fact]
    public void ComparisonCase_100PlayersTeamSize5_ShouldCompareCpSatWithLongRunningFallback()
    {
        var options = Options.Create(new TeamBalancingOptions
        {
            CpSatTimeLimitSeconds = 60,
            GreedyLocalSearchMaxIterations = int.MaxValue
        });
        var randomProvider = new DeterministicRandomProvider(0);
        var fallback = new GreedyTeamBalancer(randomProvider, options);
        var cpSat = new CpSatAverageEloTeamBalancer(fallback, randomProvider, options);
        var ratings = Enumerable
            .Range(0, 100)
            .Select(index => 750 + index * 41 % 1700)
            .ToArray();
        var players = Players(ratings);
        const int teamSize = 5;

        var fallbackStopwatch = Stopwatch.StartNew();
        var fallbackTeams = fallback.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        fallbackStopwatch.Stop();

        var cpSatStopwatch = Stopwatch.StartNew();
        var cpSatTeams = cpSat.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());
        cpSatStopwatch.Stop();

        WriteComparison(
            "100 players, team size 5, long fallback",
            fallbackStopwatch.Elapsed,
            CalculateTeamTotalGap(fallbackTeams),
            cpSatStopwatch.Elapsed,
            CalculateTeamTotalGap(cpSatTeams),
            cpSat.LastDiagnostics);
        AssertValidBalancedTeams(fallbackTeams, players, teamSize);
        AssertValidBalancedTeams(cpSatTeams, players, teamSize);
    }

    [Fact]
    public void VeryLargeCase_300PlayersTeamSize5_ShouldCompleteWithinSolverTimeLimitAndReturnValidTeams()
    {
        var balancer = CreateBalancer(new TeamBalancingOptions { CpSatTimeLimitSeconds = 300 });
        var ratings = Enumerable
            .Range(0, 300)
            .Select(index => 700 + index * 37 % 1800)
            .ToArray();
        var players = Players(ratings);
        const int teamSize = 5;
        var stopwatch = Stopwatch.StartNew();

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        stopwatch.Stop();
        WriteElapsed("300 players, team size 5", stopwatch.Elapsed, balancer.LastDiagnostics);
        Assert.Equal(60, teams.Count);
        AssertValidBalancedTeams(teams, players, teamSize);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(330), $"Elapsed: {stopwatch.Elapsed}");
    }

    [Fact]
    public void StressCase_1000PlayersTeamSize5_ShouldCompleteWithinSolverTimeLimitAndReturnValidTeams()
    {
        var balancer = CreateBalancer(new TeamBalancingOptions { CpSatTimeLimitSeconds = 20 });
        var ratings = Enumerable
            .Range(0, 1000)
            .Select(index => 600 + index * 53 % 2400)
            .ToArray();
        var players = Players(ratings);
        const int teamSize = 5;
        var stopwatch = Stopwatch.StartNew();

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        stopwatch.Stop();
        WriteElapsed("1000 players, team size 5", stopwatch.Elapsed, balancer.LastDiagnostics);
        Console.WriteLine($"1000 players, team size 5 returned gap: {CalculateTeamTotalGap(teams)}");
        output.WriteLine($"1000 players, team size 5 returned gap: {CalculateTeamTotalGap(teams)}");
        Assert.Equal(200, teams.Count);
        AssertValidBalancedTeams(teams, players, teamSize);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Elapsed: {stopwatch.Elapsed}");
    }

    private void WriteElapsed(
        string scenario,
        TimeSpan elapsed,
        CpSatTeamBalancingDiagnostics? diagnostics = null)
    {
        var message = diagnostics is null
            ? $"{scenario} elapsed: {elapsed}"
            : $"{scenario} elapsed: {elapsed}; status: {diagnostics.Status}; gap: {diagnostics.TeamTotalGap}; bound: {diagnostics.BestObjectiveBound}";

        output.WriteLine(message);
        Console.WriteLine(message);
    }

    private void WriteComparison(
        string scenario,
        TimeSpan fallbackElapsed,
        int fallbackGap,
        TimeSpan cpSatElapsed,
        int cpSatGap,
        CpSatTeamBalancingDiagnostics? diagnostics)
    {
        var message = diagnostics is null
            ? $"{scenario}; fallback elapsed: {fallbackElapsed}; fallback gap: {fallbackGap}; CP-SAT elapsed: {cpSatElapsed}; CP-SAT gap: {cpSatGap}"
            : $"{scenario}; fallback elapsed: {fallbackElapsed}; fallback gap: {fallbackGap}; CP-SAT elapsed: {cpSatElapsed}; CP-SAT gap: {cpSatGap}; status: {diagnostics.Status}; bound: {diagnostics.BestObjectiveBound}";

        output.WriteLine(message);
        Console.WriteLine(message);
    }

    private void WriteUpperBoundComparison(
        string scenario,
        TimeSpan fallbackElapsed,
        int fallbackGap,
        TimeSpan plainElapsed,
        int plainGap,
        CpSatTeamBalancingDiagnostics? plainDiagnostics,
        TimeSpan upperBoundElapsed,
        int upperBoundGap,
        CpSatTeamBalancingDiagnostics? upperBoundDiagnostics)
    {
        var message =
            $"{scenario}; fallback elapsed: {fallbackElapsed}; fallback gap: {fallbackGap}; " +
            $"plain CP-SAT elapsed: {plainElapsed}; plain returned gap: {plainGap}; plain solver gap: {plainDiagnostics?.TeamTotalGap}; plain status: {plainDiagnostics?.Status}; plain bound: {plainDiagnostics?.BestObjectiveBound}; " +
            $"upper-bound CP-SAT elapsed: {upperBoundElapsed}; upper-bound returned gap: {upperBoundGap}; upper-bound solver gap: {upperBoundDiagnostics?.TeamTotalGap}; upper-bound status: {upperBoundDiagnostics?.Status}; upper-bound bound: {upperBoundDiagnostics?.BestObjectiveBound}";

        output.WriteLine(message);
        Console.WriteLine(message);
    }

    private void WriteHintComparison(
        string scenario,
        TimeSpan fallbackElapsed,
        int fallbackGap,
        TimeSpan plainElapsed,
        int plainGap,
        CpSatTeamBalancingDiagnostics? plainDiagnostics,
        TimeSpan hintedElapsed,
        int hintedGap,
        CpSatTeamBalancingDiagnostics? hintedDiagnostics)
    {
        var message =
            $"{scenario}; fallback elapsed: {fallbackElapsed}; fallback gap: {fallbackGap}; " +
            $"plain CP-SAT elapsed: {plainElapsed}; plain gap: {plainGap}; plain status: {plainDiagnostics?.Status}; plain bound: {plainDiagnostics?.BestObjectiveBound}; " +
            $"hinted CP-SAT elapsed: {hintedElapsed}; hinted gap: {hintedGap}; hinted status: {hintedDiagnostics?.Status}; hinted bound: {hintedDiagnostics?.BestObjectiveBound}";

        output.WriteLine(message);
        Console.WriteLine(message);
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
