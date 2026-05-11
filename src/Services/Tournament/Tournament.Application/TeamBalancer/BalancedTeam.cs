namespace Tournament.Application.TeamBalancer;

public sealed record BalancedTeam(
    string Name,
    Guid CaptainPlayerId,
    IReadOnlyCollection<BalancedTeamMember> Members,
    double AverageElo);
