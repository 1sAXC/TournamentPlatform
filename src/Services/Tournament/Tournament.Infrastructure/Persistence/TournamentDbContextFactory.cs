using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tournament.Infrastructure.Persistence;

public sealed class TournamentDbContextFactory : IDesignTimeDbContextFactory<TournamentDbContext>
{
    public TournamentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TournamentDb")
            ?? Environment.GetEnvironmentVariable("TOURNAMENT_DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5434;Database=tournament_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<TournamentDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            options => options.MigrationsAssembly(typeof(TournamentDbContext).Assembly.FullName));

        return new TournamentDbContext(optionsBuilder.Options);
    }
}
