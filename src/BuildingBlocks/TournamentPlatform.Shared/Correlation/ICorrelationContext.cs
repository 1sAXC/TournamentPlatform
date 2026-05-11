namespace TournamentPlatform.Shared.Correlation;

public interface ICorrelationContext
{
    string? CorrelationId { get; set; }
}
