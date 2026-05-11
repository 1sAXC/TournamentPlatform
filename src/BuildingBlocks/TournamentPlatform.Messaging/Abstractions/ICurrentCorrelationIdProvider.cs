namespace TournamentPlatform.Messaging.Abstractions;

public interface ICurrentCorrelationIdProvider
{
    string? CorrelationId { get; }
}
