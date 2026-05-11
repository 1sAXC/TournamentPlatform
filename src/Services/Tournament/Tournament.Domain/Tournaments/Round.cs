using TournamentPlatform.Contracts.Enums;

namespace Tournament.Domain.Tournaments;

public sealed class Round
{
    private readonly List<Match> _matches = [];

    private Round()
    {
    }

    private Round(Guid tournamentId, int number, BracketType bracketType, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        TournamentId = tournamentId;
        Number = number;
        BracketType = bracketType;
        Status = RoundStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public int Number { get; private set; }
    public BracketType BracketType { get; private set; }
    public RoundStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public IReadOnlyCollection<Match> Matches => _matches.AsReadOnly();

    public static Round Create(Guid tournamentId, int number, BracketType bracketType, DateTime createdAtUtc)
    {
        return new Round(tournamentId, number, bracketType, createdAtUtc);
    }

    public void Start()
    {
        Status = RoundStatus.InProgress;
    }

    public void Complete(DateTime completedAtUtc)
    {
        Status = RoundStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    public void AddMatch(Match match)
    {
        _matches.Add(match);
    }
}
