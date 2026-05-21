namespace Tournament.Application.TeamBalancer;

public sealed class TeamBalancingOptions
{
    public int CpSatTimeLimitSeconds { get; set; } = 10;

    public int GreedyLocalSearchMaxIterations { get; set; } = 10_000;
}
