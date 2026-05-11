using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class TournamentParticipantConfiguration : IEntityTypeConfiguration<TournamentParticipant>
{
    public void Configure(EntityTypeBuilder<TournamentParticipant> builder)
    {
        builder.ToTable("TournamentParticipants");

        builder.HasKey(participant => participant.Id);

        builder.Property(participant => participant.Id)
            .ValueGeneratedNever();

        builder.Property(participant => participant.PlayerNickname)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(participant => participant.RegisteredAtUtc)
            .IsRequired();

        builder.Property(participant => participant.IsActive)
            .IsRequired();

        builder.HasIndex(participant => new { participant.TournamentId, participant.PlayerId })
            .IsUnique()
            .HasFilter("\"IsActive\" = TRUE");

        builder.HasIndex(participant => participant.PlayerId);
        builder.HasIndex(participant => participant.IsActive);
    }
}
