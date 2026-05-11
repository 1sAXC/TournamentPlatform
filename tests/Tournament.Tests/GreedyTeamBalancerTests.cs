using Tournament.Application.TeamBalancer;

namespace Tournament.Tests;

public sealed class GreedyTeamBalancerTests
{
    [Fact]
    public void TeamSizeOne_CreatesOneTeamPerPlayer()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));
        var players = Players(3);

        var teams = balancer.BuildTeams(players, 1, "CS2", Guid.NewGuid());

        Assert.Equal(3, teams.Count);
        Assert.All(teams, team => Assert.Single(team.Members));
        AssertNoDuplicatePlayers(teams);
    }

    [Fact]
    public void TeamSizeTwo_CreatesTeamsWithTwoPlayers()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));
        var players = Players(4);

        var teams = balancer.BuildTeams(players, 2, "CS2", Guid.NewGuid());

        Assert.Equal(2, teams.Count);
        Assert.All(teams, team => Assert.Equal(2, team.Members.Count));
        AssertNoDuplicatePlayers(teams);
    }

    [Fact]
    public void TeamSizeFive_CreatesTeamsWithFivePlayers()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));
        var players = Players(10);

        var teams = balancer.BuildTeams(players, 5, "CS2", Guid.NewGuid());

        Assert.Equal(2, teams.Count);
        Assert.All(teams, team => Assert.Equal(5, team.Members.Count));
        AssertNoDuplicatePlayers(teams);
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

        var team = balancer.BuildTeams(players, 2, "CS2", Guid.NewGuid()).Single();
        var captain = team.Members.Single(member => member.PlayerId == team.CaptainPlayerId);

        Assert.Equal(team.Members.Max(member => member.Elo), captain.Elo);
        Assert.Equal(captain.Nickname, team.Name);
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

        Assert.Equal(1200, captain.Elo);
        Assert.Contains(captain.Nickname, ["HighA", "HighB"]);
        Assert.Equal(captain.Nickname, team.Name);
    }

    [Fact]
    public void NonDivisiblePlayers_Throws()
    {
        var balancer = new GreedyTeamBalancer(new DeterministicRandomProvider(0));

        Assert.Throws<TeamBalancingException>(() =>
            balancer.BuildTeams(Players(3), 2, "CS2", Guid.NewGuid()));
    }

    private static IReadOnlyCollection<PlayerForBalancing> Players(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => new PlayerForBalancing(
                Guid.NewGuid(),
                $"Player{index}",
                900 + index * 10))
            .ToArray();
    }

    private static void AssertNoDuplicatePlayers(IReadOnlyCollection<BalancedTeam> teams)
    {
        var playerIds = teams.SelectMany(team => team.Members.Select(member => member.PlayerId)).ToArray();
        Assert.Equal(playerIds.Length, playerIds.Distinct().Count());
    }

    private sealed class DeterministicRandomProvider(int value) : IRandomProvider
    {
        public int Next(int maxExclusive)
        {
            return value % maxExclusive;
        }
    }
}
