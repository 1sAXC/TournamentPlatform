using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rating.Infrastructure.Persistence;

public sealed class RatingDbContextFactory : IDesignTimeDbContextFactory<RatingDbContext>
{
    public RatingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RatingDb")
            ?? Environment.GetEnvironmentVariable("RATING_DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5435;Database=rating_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<RatingDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            options => options.MigrationsAssembly(typeof(RatingDbContext).Assembly.FullName));

        return new RatingDbContext(optionsBuilder.Options);
    }
}
