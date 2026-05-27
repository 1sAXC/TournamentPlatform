using TournamentPlatform.Contracts.Enums;

namespace Tournament.Domain.Tournaments;

public sealed class Tournament
{
    private readonly List<TournamentParticipant> _participants = [];
    private readonly List<Team> _teams = [];
    private readonly List<Round> _rounds = [];
    private readonly List<SwissStanding> _swissStandings = [];
    private readonly List<DoubleEliminationStanding> _doubleEliminationStandings = [];

    private Tournament()
    {
    }

    private Tournament(
        string title,
        string normalizedTitle,
        string? description,
        string disciplineCode,
        TournamentFormat format,
        int? swissRounds,
        int teamSize,
        int maxPlayers,
        Guid organizerId,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        Title = title;
        NormalizedTitle = normalizedTitle;
        Description = description;
        DisciplineCode = disciplineCode;
        Format = format;
        SwissRounds = swissRounds;
        TeamSize = teamSize;
        MaxPlayers = maxPlayers;
        OrganizerId = organizerId;
        Status = TournamentStatus.Open;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string NormalizedTitle { get; private set; } = default!;
    public string? Description { get; private set; }
    public string DisciplineCode { get; private set; } = default!;
    public TournamentFormat Format { get; private set; }
    public int? SwissRounds { get; private set; }
    public int TeamSize { get; private set; }
    public int MaxPlayers { get; private set; }
    public Guid OrganizerId { get; private set; }
    public TournamentStatus Status { get; private set; }
    public int CurrentRoundNumber { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<TournamentParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<Team> Teams => _teams.AsReadOnly();
    public IReadOnlyCollection<Round> Rounds => _rounds.AsReadOnly();
    public IReadOnlyCollection<SwissStanding> SwissStandings => _swissStandings.AsReadOnly();
    public IReadOnlyCollection<DoubleEliminationStanding> DoubleEliminationStandings => _doubleEliminationStandings.AsReadOnly();

    public int ActiveParticipantsCount => _participants.Count(participant => participant.IsActive);

    public bool HasFreeSlots()
    {
        return ActiveParticipantsCount < MaxPlayers;
    }

    public bool HasActiveParticipant(Guid playerId)
    {
        return _participants.Any(participant => participant.PlayerId == playerId && participant.IsActive);
    }

    public TournamentParticipant? GetActiveParticipant(Guid playerId)
    {
        return _participants.FirstOrDefault(participant => participant.PlayerId == playerId && participant.IsActive);
    }

    public TournamentParticipant RegisterParticipant(Guid playerId, string playerNickname, DateTime registeredAtUtc)
    {
        var participant = TournamentParticipant.Create(Id, playerId, playerNickname, registeredAtUtc);
        _participants.Add(participant);
        TouchConcurrencyToken();
        return participant;
    }

    public void UpdateDetails(string title, string normalizedTitle, string? description)
    {
        Title = title;
        NormalizedTitle = normalizedTitle;
        Description = description;
        TouchConcurrencyToken();
    }

    public void MarkFull()
    {
        Status = TournamentStatus.Full;
        TouchConcurrencyToken();
    }

    public void Reopen()
    {
        Status = TournamentStatus.Open;
        TouchConcurrencyToken();
    }

    public void Start(DateTime startedAtUtc)
    {
        Status = TournamentStatus.InProgress;
        StartedAtUtc = startedAtUtc;
        CurrentRoundNumber = 1;
        TouchConcurrencyToken();
    }

    public void AddTeams(IEnumerable<Team> teams)
    {
        _teams.AddRange(teams);
        TouchConcurrencyToken();
    }

    public void AddRound(Round round)
    {
        _rounds.Add(round);
        TouchConcurrencyToken();
    }

    public void AddSwissStanding(SwissStanding standing)
    {
        _swissStandings.Add(standing);
        TouchConcurrencyToken();
    }

    public void AddDoubleEliminationStanding(DoubleEliminationStanding standing)
    {
        _doubleEliminationStandings.Add(standing);
        TouchConcurrencyToken();
    }

    public void Complete(DateTime completedAtUtc)
    {
        Status = TournamentStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        TouchConcurrencyToken();
    }

    public void Cancel(DateTime cancelledAtUtc)
    {
        Status = TournamentStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        TouchConcurrencyToken();
    }

    public void SoftDelete(DateTime deletedAtUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        TouchConcurrencyToken();
    }

    public void TouchConcurrencyToken()
    {
        RowVersion = Guid.NewGuid().ToByteArray();
    }

    public static Tournament Create(
        string title,
        string normalizedTitle,
        string? description,
        string disciplineCode,
        TournamentFormat format,
        int? swissRounds,
        int teamSize,
        int maxPlayers,
        Guid organizerId,
        DateTime createdAtUtc)
    {
        var tournament = new Tournament(
            title,
            normalizedTitle,
            description,
            disciplineCode,
            format,
            swissRounds,
            teamSize,
            maxPlayers,
            organizerId,
            createdAtUtc);

        tournament.TouchConcurrencyToken();
        return tournament;
    }
}
