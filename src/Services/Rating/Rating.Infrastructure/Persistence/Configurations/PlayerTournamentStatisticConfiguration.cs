using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rating.Domain.Ratings;

namespace Rating.Infrastructure.Persistence.Configurations;

public sealed class PlayerTournamentStatisticConfiguration : IEntityTypeConfiguration<PlayerTournamentStatistic>
{
    public void Configure(EntityTypeBuilder<PlayerTournamentStatistic> builder)
    {
        builder.ToTable("PlayerTournamentStatistics");

        builder.HasKey(statistic => statistic.Id);

        builder.Property(statistic => statistic.DisciplineCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(statistic => statistic.CreatedAtUtc)
            .IsRequired();

        builder.Property(statistic => statistic.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(statistic => statistic.PlayerId);
        builder.HasIndex(statistic => statistic.TournamentId);
        builder.HasIndex(statistic => statistic.DisciplineCode);
        builder.HasIndex(statistic => new { statistic.PlayerId, statistic.TournamentId, statistic.DisciplineCode })
            .IsUnique();
    }
}
