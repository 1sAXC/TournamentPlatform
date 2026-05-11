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
    public bool IsEliminated { get; private set; }

    public static DoubleEliminationStanding Create(Guid tournamentId, Guid teamId)
    {
        return new DoubleEliminationStanding(tournamentId, teamId);
    }

    public void AddLoss()
    {
        Losses++;
        if (Losses >= 2)
        {
            IsEliminated = true;
        }
    }
}
