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
    public int? WinnerScore { get; init; }
    public int? LoserScore { get; init; }
    public bool IsTechnicalDefeat { get; init; }
}
