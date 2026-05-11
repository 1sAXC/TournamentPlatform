using Auth.Domain.Users;
using Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure.Seed;

public static class AuthDatabaseSeeder
{
    private static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAuthDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var adminPassword = configuration["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("AdminSeed:Password is not configured.");
        }

        var adminEmail = "admin@tournament.local";
        var normalizedAdminEmail = adminEmail.ToUpperInvariant();

        var adminExists = await dbContext.Users
            .AnyAsync(user => user.NormalizedEmail == normalizedAdminEmail, cancellationToken);

        if (adminExists)
        {
            return;
        }

        var admin = User.CreateAdmin(
            AdminUserId,
            adminEmail,
            passwordHash: "temporary",
            DateTime.UtcNow);

        var passwordHash = passwordHasher.HashPassword(admin, adminPassword);
        admin.SetPasswordHash(passwordHash);
        admin.ClearDomainEvents();

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
