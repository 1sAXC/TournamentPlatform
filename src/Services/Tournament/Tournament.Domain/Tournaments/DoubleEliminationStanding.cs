namespace Tournament.Domain.Tournaments;

public sealed class DoubleEliminationStanding
{
    private DoubleEliminationStanding()
    {
    }

    private DoubleEliminationStanding(Guid tournamentId, Guid teamId)
    {
        Id = Guid.NewGuid();
        TournamentId = tournamentId;
        TeamId = teamId;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid TeamId { get; private set; }
    public int Losses { get; private set; }

    // Double-elimination convention: a team is out of the bracket after two
    // losses. Derived from Losses on demand so the schema doesn't carry a
    // duplicate persisted bit. The Configuration uses .Ignore() for this.
    public bool IsEliminated => Losses >= 2;

    public static DoubleEliminationStanding Create(Guid tournamentId, Guid teamId)
    {
        return new DoubleEliminationStanding(tournamentId, teamId);
    }

    public void AddLoss()
    {
        Losses++;
    }
}
