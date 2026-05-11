namespace Tournament.Domain.Tournaments;

public sealed class PlayerRatingProjection
{
    private PlayerRatingProjection()
    {
    }

    private PlayerRatingProjection(Guid playerId, string disciplineCode, int elo, DateTime updatedAtUtc)
    {
        Id = Guid.NewGuid();
        PlayerId = playerId;
        DisciplineCode = disciplineCode;
        Elo = elo;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public string DisciplineCode { get; private set; } = default!;
    public int Elo { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static PlayerRatingProjection Create(Guid playerId, string disciplineCode, int elo, DateTime updatedAtUtc)
    {
        return new PlayerRatingProjection(playerId, disciplineCode, elo, updatedAtUtc);
    }

    public void UpdateElo(int elo, DateTime updatedAtUtc)
    {
        Elo = elo;
        UpdatedAtUtc = updatedAtUtc;
    }
}
