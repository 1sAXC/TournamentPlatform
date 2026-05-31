namespace Rating.Domain.Ratings;

public sealed class PlayerRating
{
    private PlayerRating()
    {
    }

    private PlayerRating(Guid playerId, string disciplineCode, int elo, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        PlayerId = playerId;
        DisciplineCode = disciplineCode;
        Elo = elo;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public string DisciplineCode { get; private set; } = default!;
    public int Elo { get; private set; }
    public int Wins { get; private set; }
    public int Losses { get; private set; }
    public int MatchesPlayed { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PlayerRating CreateInitial(Guid playerId, string disciplineCode, int initialElo, DateTime createdAtUtc)
    {
        return new PlayerRating(playerId, disciplineCode, initialElo, createdAtUtc);
    }

    public void MarkDeleted(DateTime deletedAtUtc)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        UpdatedAtUtc = deletedAtUtc;
    }

    public void Restore(DateTime restoredAtUtc)
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        UpdatedAtUtc = restoredAtUtc;
    }

    public void ApplyMatchResult(int newElo, bool isWin, DateTime updatedAtUtc)
    {
        Elo = Math.Max(100, newElo);
        MatchesPlayed++;
        if (isWin)
        {
            Wins++;
        }
        else
        {
            Losses++;
        }

        UpdatedAtUtc = updatedAtUtc;
    }
}
