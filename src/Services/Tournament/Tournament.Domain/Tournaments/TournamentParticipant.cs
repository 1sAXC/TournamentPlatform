namespace Tournament.Domain.Tournaments;

public sealed class TournamentParticipant
{
    private TournamentParticipant()
    {
    }

    private TournamentParticipant(
        Guid tournamentId,
        Guid playerId,
        string playerNickname,
        DateTime registeredAtUtc)
    {
        Id = Guid.NewGuid();
        TournamentId = tournamentId;
        PlayerId = playerId;
        PlayerNickname = playerNickname;
        RegisteredAtUtc = registeredAtUtc;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid PlayerId { get; private set; }
    public string PlayerNickname { get; private set; } = default!;
    public DateTime RegisteredAtUtc { get; private set; }
    public DateTime? LeftAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    public static TournamentParticipant Create(
        Guid tournamentId,
        Guid playerId,
        string playerNickname,
        DateTime registeredAtUtc)
    {
        return new TournamentParticipant(tournamentId, playerId, playerNickname, registeredAtUtc);
    }

    public void Leave(DateTime leftAtUtc)
    {
        IsActive = false;
        LeftAtUtc = leftAtUtc;
    }
}
