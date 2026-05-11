using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TournamentPlatform.Messaging.Inbox;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(message => new { message.EventId, message.ConsumerName });

        builder.Property(message => message.ConsumerName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(message => message.ProcessedAtUtc)
            .IsRequired();
    }
}
