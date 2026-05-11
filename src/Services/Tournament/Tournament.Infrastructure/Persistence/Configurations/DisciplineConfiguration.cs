using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Common;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class DisciplineConfiguration : IEntityTypeConfiguration<Discipline>
{
    public void Configure(EntityTypeBuilder<Discipline> builder)
    {
        builder.ToTable("Disciplines");

        builder.HasKey(discipline => discipline.Code);

        builder.Property(discipline => discipline.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(discipline => discipline.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(discipline => discipline.IsActive)
            .IsRequired();

        builder.Property(discipline => discipline.AllowedTeamSizes)
            .IsRequired();

        builder.HasIndex(discipline => discipline.IsActive);

        builder.HasData(
            Discipline.Create(DisciplineCodes.CS2, "CS2", [1, 2, 5]),
            Discipline.Create(DisciplineCodes.PUBG, "PUBG", [1, 2, 5]),
            Discipline.Create(DisciplineCodes.Valorant, "Valorant", [1, 2, 5]),
            Discipline.Create(DisciplineCodes.Standoff2, "Standoff2", [1, 2, 5]));
    }
}
