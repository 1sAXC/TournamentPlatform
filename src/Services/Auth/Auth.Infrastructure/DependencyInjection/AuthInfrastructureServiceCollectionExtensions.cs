using Auth.Application.Admin.Services;
using Auth.Application.Auth.Abstractions;
using Auth.Application.Auth.Services;
using Auth.Domain.Users;
using Auth.Infrastructure.Auth;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TournamentPlatform.Messaging.Abstractions;
using TournamentPlatform.Messaging.Outbox;

namespace Auth.Infrastructure.DependencyInjection;

public static class AuthInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AuthDb")
            ?? throw new InvalidOperationException("Connection string 'AuthDb' is not configured.");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName)));

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IOutboxMessageStore, AuthOutboxMessageStore>();
        services.AddScoped<IAuthUserRepository, AuthUserRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUsersService, AdminUsersService>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        return services;
    }
}
