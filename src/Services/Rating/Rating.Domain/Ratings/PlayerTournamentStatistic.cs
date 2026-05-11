namespace Rating.Domain.Ratings;

public sealed class PlayerTournamentStatistic
{
    private PlayerTournamentStatistic()
    {
    }

    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public Guid TournamentId { get; private set; }
    public string DisciplineCode { get; private set; } = default!;
    public int Placement { get; private set; }
    public int Wins { get; private set; }
    public int Losses { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
