namespace Tournament.Domain.Tournaments;

public sealed class Team
{
    private readonly List<TeamMember> _members = [];

    private Team()
    {
    }

    private Team(
        Guid tournamentId,
        string name,
        Guid captainPlayerId,
        int seed,
        double averageElo,
        IEnumerable<TeamMember> members)
    {
        Id = Guid.NewGuid();
        TournamentId = tournamentId;
        Name = name;
        CaptainPlayerId = captainPlayerId;
        Seed = seed;
        AverageElo = averageElo;
        _members.AddRange(members);
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid CaptainPlayerId { get; private set; }
    public int Seed { get; private set; }
    public double AverageElo { get; private set; }
    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();

    public static Team Create(
        Guid tournamentId,
        string name,
        Guid captainPlayerId,
        int seed,
        double averageElo,
        IEnumerable<TeamMember> members)
    {
        return new Team(tournamentId, name, captainPlayerId, seed, averageElo, members);
    }
}
