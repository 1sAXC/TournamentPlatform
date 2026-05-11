namespace TournamentPlatform.Shared.Correlation;

public sealed class CorrelationContext : ICorrelationContext
{
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    public string? CorrelationId
    {
        get => CurrentCorrelationId.Value;
        set => CurrentCorrelationId.Value = value;
    }
}
