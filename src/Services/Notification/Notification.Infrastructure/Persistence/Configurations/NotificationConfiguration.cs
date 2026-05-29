using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.RecipientUserId).IsRequired();

        builder.Property(notification => notification.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(notification => notification.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(notification => notification.Body)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(notification => notification.LinkUrl)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(notification => notification.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(notification => notification.SourceEventId).IsRequired();
        builder.Property(notification => notification.CreatedAtUtc).IsRequired();

        // Recent-first listing by recipient. Composite index speeds up the
        // primary list query (where recipient = ? order by createdAt desc).
        builder.HasIndex(notification => new { notification.RecipientUserId, notification.CreatedAtUtc });

        // Idempotency: the same source integration event must not produce
        // two notifications for the same recipient even if the message gets
        // redelivered. Inbox handles consumer-level dedup, this is a
        // belt-and-suspenders guarantee at the data layer.
        builder.HasIndex(notification => new { notification.SourceEventId, notification.RecipientUserId })
            .IsUnique();
    }
}
