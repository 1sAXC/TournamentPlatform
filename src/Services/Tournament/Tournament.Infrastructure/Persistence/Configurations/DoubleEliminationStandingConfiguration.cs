using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class DoubleEliminationStandingConfiguration : IEntityTypeConfiguration<DoubleEliminationStanding>
{
    public void Configure(EntityTypeBuilder<DoubleEliminationStanding> builder)
    {
        builder.ToTable("DoubleEliminationStandings");

        builder.HasKey(standing => standing.Id);

        builder.Property(standing => standing.Id)
            .ValueGeneratedNever();

        // Derived from Losses, see DoubleEliminationStanding.IsEliminated.
        builder.Ignore(standing => standing.IsEliminated);

        builder.HasIndex(standing => new { standing.TournamentId, standing.TeamId })
            .IsUnique();
    }
}
