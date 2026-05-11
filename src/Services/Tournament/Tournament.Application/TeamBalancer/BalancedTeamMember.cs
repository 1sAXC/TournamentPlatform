namespace Tournament.Application.TeamBalancer;

public sealed record BalancedTeamMember(
    Guid PlayerId,
    string Nickname,
    int Elo);
