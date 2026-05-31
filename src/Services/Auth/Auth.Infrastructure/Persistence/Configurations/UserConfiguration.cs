using Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TournamentPlatform.Contracts.Enums;

namespace Auth.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(user => user.Role);

        builder.Property(user => user.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(user => user.Status);

        builder.Property(user => user.Nickname)
            .HasMaxLength(64);

        builder.Property(user => user.NormalizedNickname)
            .HasMaxLength(64);

        builder.HasIndex(user => user.NormalizedNickname)
            .IsUnique()
            .HasFilter("\"NormalizedNickname\" IS NOT NULL");

        builder.Property(user => user.OrganizerName)
            .HasMaxLength(128);

        builder.Property(user => user.NormalizedOrganizerName)
            .HasMaxLength(128);

        builder.Property(user => user.ContactHandle)
            .HasMaxLength(64);

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();

        // RowVersion is kept as a plain byte[] column for legacy compatibility
        // with the initial schema. It is NOT a concurrency token: nothing in
        // the domain updates it, so configuring it as one would be a no-op
        // (EF would compare empty-against-empty forever). If optimistic
        // concurrency on User becomes important, switch to xmin via
        // .UseXminAsConcurrencyToken() and update mutating methods.
        builder.Property(user => user.RowVersion)
            .HasDefaultValue(Array.Empty<byte>());

        builder.Ignore(user => user.DomainEvents);
    }
}
