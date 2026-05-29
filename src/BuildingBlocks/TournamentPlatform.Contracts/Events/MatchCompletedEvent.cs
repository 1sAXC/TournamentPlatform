namespace TournamentPlatform.Contracts.Events;

public sealed record MatchCompletedEvent : IntegrationEvent
{
    public MatchCompletedEvent()
    {
        EventType = nameof(MatchCompletedEvent);
    }

    public Guid MatchId { get; init; }
    public Guid TournamentId { get; init; }
    public string DisciplineCode { get; init; } = default!;
    public int TeamSize { get; init; }
    public Guid WinnerTeamId { get; init; }
    public Guid LoserTeamId { get; init; }
    public IReadOnlyCollection<MatchCompletedPlayerDto> WinnerPlayers { get; init; } = [];
    public IReadOnlyCollection<MatchCompletedPlayerDto> LoserPlayers { get; init; } = [];
    /// <summary>Rounds — sum across all maps in the series. Used by Rating.</summary>
    public int? WinnerScore { get; init; }
    public int? LoserScore { get; init; }
    /// <summary>Maps won in the series (e.g. 2 / 1 for Bo3). Display-only.</summary>
    public int? WinnerMaps { get; init; }
    public int? LoserMaps { get; init; }
    public bool IsTechnicalDefeat { get; init; }
}
