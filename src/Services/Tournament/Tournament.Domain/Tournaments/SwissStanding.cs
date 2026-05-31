namespace Tournament.Domain.Tournaments;

public sealed class SwissStanding
{
    private SwissStanding()
    {
    }

    private SwissStanding(Guid tournamentId, Guid teamId)
    {
        Id = Guid.NewGuid();
        TournamentId = tournamentId;
        TeamId = teamId;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid TeamId { get; private set; }
    public int Points { get; private set; }
    public int Wins { get; private set; }
    public int Losses { get; private set; }

    public static SwissStanding Create(Guid tournamentId, Guid teamId)
    {
        return new SwissStanding(tournamentId, teamId);
    }

    public void AddWin()
    {
        Wins++;
        Points += 3;
    }

    public void AddLoss()
    {
        Losses++;
    }
}
