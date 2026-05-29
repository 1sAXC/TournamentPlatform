namespace TournamentPlatform.Contracts.Events;

/// <summary>
/// Raised by the Tournament service every time a new round is created — both
/// on tournament start (initial round) and on subsequent rounds (auto-generated
/// in single/double elimination, or manually created in Swiss).
///
/// Carries the full team rosters of the new round so downstream consumers can
/// fan out per-match notifications without calling back into Tournament.Api.
/// </summary>
public sealed record RoundCreatedEvent : IntegrationEvent
{
    public RoundCreatedEvent()
    {
        EventType = nameof(RoundCreatedEvent);
    }

    public Guid TournamentId { get; init; }
    public string TournamentTitle { get; init; } = default!;
    public string DisciplineCode { get; init; } = default!;
    public Guid OrganizerId { get; init; }

    public Guid RoundId { get; init; }
    public int RoundNumber { get; init; }
    public string BracketType { get; init; } = default!;

    /// <summary>
    /// Teams that participate in any of the matches of this round.
    /// Matches reference teams by id; this collection provides the rosters
    /// (nicknames, captain flag) needed for fan-out logic in consumers.
    /// </summary>
    public IReadOnlyCollection<EventTeamDto> Teams { get; init; } = [];

    /// <summary>
    /// All matches of the newly created round. Each match references team ids;
    /// look the teams up in <see cref="Teams"/> to get the rosters.
    /// </summary>
    public IReadOnlyCollection<EventMatchDto> Matches { get; init; } = [];
}
