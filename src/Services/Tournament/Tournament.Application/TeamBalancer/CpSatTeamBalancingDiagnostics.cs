namespace Tournament.Application.TeamBalancer;

public sealed record CpSatTeamBalancingDiagnostics(
    string Status,
    long? TeamTotalGap,
    double? BestObjectiveBound);
