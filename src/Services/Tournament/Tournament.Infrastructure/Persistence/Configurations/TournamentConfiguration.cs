using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class TournamentConfiguration : IEntityTypeConfiguration<Domain.Tournaments.Tournament>
{
    public void Configure(EntityTypeBuilder<Domain.Tournaments.Tournament> builder)
    {
        builder.ToTable("Tournaments");

        builder.HasKey(tournament => tournament.Id);

        builder.Property(tournament => tournament.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(tournament => tournament.NormalizedTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(tournament => tournament.Description)
            .HasMaxLength(4000);

        builder.Property(tournament => tournament.DisciplineCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(tournament => tournament.Format)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(tournament => tournament.Status)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(tournament => tournament.CreatedAtUtc)
            .IsRequired();

        builder.Property(tournament => tournament.IsDeleted)
            .IsRequired();

        builder.Property(tournament => tournament.RowVersion)
            .IsConcurrencyToken()
            .HasDefaultValue(Array.Empty<byte>());

        builder.HasMany(tournament => tournament.Participants)
            .WithOne()
            .HasForeignKey(participant => participant.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(tournament => tournament.Teams)
            .WithOne()
            .HasForeignKey(team => team.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(tournament => tournament.Rounds)
            .WithOne()
            .HasForeignKey(round => round.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(tournament => tournament.SwissStandings)
            .WithOne()
            .HasForeignKey(standing => standing.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(tournament => tournament.DoubleEliminationStandings)
            .WithOne()
            .HasForeignKey(standing => standing.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(tournament => tournament.NormalizedTitle)
            .IsUnique();

        builder.HasIndex(tournament => tournament.OrganizerId);
        builder.HasIndex(tournament => tournament.Status);
        builder.HasIndex(tournament => tournament.DisciplineCode);
        builder.HasIndex(tournament => tournament.IsDeleted);
    }
}
