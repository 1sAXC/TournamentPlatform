namespace TournamentPlatform.Contracts.Events;

public sealed record EventRoundDto
{
    public Guid RoundId { get; init; }
    public int Number { get; init; }
    public string BracketType { get; init; } = default!;
    public IReadOnlyCollection<EventMatchDto> Matches { get; init; } = [];
}
