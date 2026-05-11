namespace Tournament.Application.Tournaments.Exceptions;

public sealed class TournamentPersistenceConflictException : Exception
{
    public TournamentPersistenceConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
