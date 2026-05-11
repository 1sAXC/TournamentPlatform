using Microsoft.EntityFrameworkCore.Storage;
using Tournament.Application.Tournaments.Abstractions;

namespace Tournament.Infrastructure.Persistence;

public sealed class TournamentEfTransaction(IDbContextTransaction transaction) : ITournamentTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return transaction.CommitAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return transaction.DisposeAsync();
    }
}
