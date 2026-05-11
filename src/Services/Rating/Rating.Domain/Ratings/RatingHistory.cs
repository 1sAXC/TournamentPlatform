namespace Rating.Domain.Ratings;

public sealed class RatingHistory
{
    private RatingHistory()
    {
    }

    private RatingHistory(
        Guid playerId,
        string disciplineCode,
        int oldElo,
        int newElo,
        Guid? matchId,
        Guid? tournamentId,
        string reason,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        PlayerId = playerId;
        DisciplineCode = disciplineCode;
        OldElo = oldElo;
        NewElo = newElo;
        Delta = newElo - oldElo;
        MatchId = matchId;
        TournamentId = tournamentId;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public string DisciplineCode { get; private set; } = default!;
    public int OldElo { get; private set; }
    public int NewElo { get; private set; }
    public int Delta { get; private set; }
    public Guid? MatchId { get; private set; }
    public Guid? TournamentId { get; private set; }
    public string Reason { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    public static RatingHistory CreateInitial(Guid playerId, string disciplineCode, int elo, DateTime createdAtUtc)
    {
        return new RatingHistory(
            playerId,
            disciplineCode,
            elo,
            elo,
            matchId: null,
            tournamentId: null,
            "InitialRating",
            createdAtUtc);
    }

    public static RatingHistory CreateMatchResult(
        Guid playerId,
        string disciplineCode,
        int oldElo,
        int newElo,
        Guid matchId,
        Guid tournamentId,
        DateTime createdAtUtc)
    {
        return new RatingHistory(
            playerId,
            disciplineCode,
            oldElo,
            newElo,
            matchId,
            tournamentId,
            "MatchCompleted",
            createdAtUtc);
    }
}
