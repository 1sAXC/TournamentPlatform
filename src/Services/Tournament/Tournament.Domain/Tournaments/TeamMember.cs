namespace Tournament.Domain.Tournaments;

public sealed class TeamMember
{
    private TeamMember()
    {
    }

    private TeamMember(Guid playerId, string nickname, int elo)
    {
        Id = Guid.NewGuid();
        PlayerId = playerId;
        Nickname = nickname;
        Elo = elo;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid PlayerId { get; private set; }
    public string Nickname { get; private set; } = default!;
    public int Elo { get; private set; }

    public static TeamMember Create(Guid playerId, string nickname, int elo)
    {
        return new TeamMember(playerId, nickname, elo);
    }
}
