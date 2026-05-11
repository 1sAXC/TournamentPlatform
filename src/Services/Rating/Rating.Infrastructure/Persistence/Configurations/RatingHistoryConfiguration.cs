using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rating.Domain.Ratings;

namespace Rating.Infrastructure.Persistence.Configurations;

public sealed class RatingHistoryConfiguration : IEntityTypeConfiguration<RatingHistory>
{
    public void Configure(EntityTypeBuilder<RatingHistory> builder)
    {
        builder.ToTable("RatingHistories");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.DisciplineCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(history => history.Reason)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(history => history.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(history => history.PlayerId);
        builder.HasIndex(history => history.DisciplineCode);
        builder.HasIndex(history => history.MatchId);
        builder.HasIndex(history => history.TournamentId);
    }
}
