using Tournament.Application.TeamBalancer;

namespace Tournament.Tests;

public sealed class GreedyTeamBalancerTests
{
    [Fact]
    public void TeamSizeOne_CreatesOneTeamPerPlayer()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));
        var players = Players([910, 920, 930]);
        const int teamSize = 1;

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        Assert.Equal(players.Count, teams.Count);
        AssertValidBalancedTeams(teams, players, teamSize);
    }

    [Fact]
    public void TeamSizeTwo_CreatesTeamsWithTwoPlayers()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));
        var players = Players([910, 920, 930, 940]);
        const int teamSize = 2;

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        Assert.Equal(2, teams.Count);
        AssertValidBalancedTeams(teams, players, teamSize);
    }

    [Fact]
    public void TeamSizeFive_CreatesTeamsWithFivePlayers()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));
        var players = Players([900, 910, 920, 930, 940, 950, 960, 970, 980, 990]);
        const int teamSize = 5;

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        Assert.Equal(2, teams.Count);
        AssertValidBalancedTeams(teams, players, teamSize);
    }

    [Fact]
    public void Captain_IsTeamMember_WithMaxElo_AndTeamNameEqualsCaptainNickname()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));
        var players = new[]
        {
            new PlayerForBalancing(Guid.NewGuid(), "Low", 900),
            new PlayerForBalancing(Guid.NewGuid(), "High", 1200)
        };

        var teams = balancer.BuildTeams(players, 2, "CS2", Guid.NewGuid());

        AssertValidBalancedTeams(teams, players, 2);
    }

    [Fact]
    public void EqualMaxElo_CaptainChosenFromMaxEloCandidates()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(1));
        var players = new[]
        {
            new PlayerForBalancing(Guid.NewGuid(), "Low", 900),
            new PlayerForBalancing(Guid.NewGuid(), "HighA", 1200),
            new PlayerForBalancing(Guid.NewGuid(), "HighB", 1200)
        };

        var team = balancer.BuildTeams(players, 3, "CS2", Guid.NewGuid()).Single();
        var captain = team.Members.Single(member => member.PlayerId == team.CaptainPlayerId);

        AssertValidBalancedTeams([team], players, 3);
        Assert.Equal(1200, captain.Elo);
        Assert.Contains(captain.Nickname, ["HighA", "HighB"]);
    }

    [Fact]
    public void NonDivisiblePlayers_Throws()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));

        Assert.Throws<TeamBalancingException>(() =>
            balancer.BuildTeams(Players([900, 1000, 1100]), 2, "CS2", Guid.NewGuid()));
    }

    [Fact]
    public void BalancedAverageCase_1000_1001_2000_2001_TeamSize2_ShouldHaveZeroTeamTotalGap()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));
        var players = Players([1000, 1001, 2000, 2001]);
        const int teamSize = 2;

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        AssertValidBalancedTeams(teams, players, teamSize);
        Assert.Equal(0, CalculateTeamTotalGap(teams));
    }

    [Fact]
    public void BalancedAverageCase_1000_1200_1400_1600_TeamSize2_ShouldHaveZeroTeamTotalGap()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));
        var players = Players([1000, 1200, 1400, 1600]);
        const int teamSize = 2;

        var teams = balancer.BuildTeams(players, teamSize, "CS2", Guid.NewGuid());

        AssertValidBalancedTeams(teams, players, teamSize);
        Assert.Equal(0, CalculateTeamTotalGap(teams));
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

    private sealed class DeterministicRandomProvider(int value) : IRandomProvider
    {
        public int Next(int maxExclusive)
        {
            return value % maxExclusive;
        }
    }
}
