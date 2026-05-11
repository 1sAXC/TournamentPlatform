namespace Tournament.Application.TeamBalancer;

public sealed record PlayerForBalancing(
    Guid PlayerId,
    string Nickname,
    int Elo);
