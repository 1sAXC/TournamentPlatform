using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TournamentPlatform.Messaging.Outbox;

namespace Auth.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.EventId)
            .IsRequired();

        builder.HasIndex(message => message.EventId)
            .IsUnique();

        builder.Property(message => message.EventType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.OccurredAtUtc)
            .IsRequired();

        builder.Property(message => message.Error)
            .HasMaxLength(2048);

        builder.Property(message => message.RetryCount)
            .IsRequired();

        builder.HasIndex(message => message.ProcessedAtUtc);
        builder.HasIndex(message => message.EventType);
    }
}
