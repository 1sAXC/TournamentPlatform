using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Notifications.Abstractions;
using Notification.Application.Notifications.Services;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using TournamentPlatform.Messaging.Abstractions;

namespace Notification.Infrastructure.DependencyInjection;

public static class NotificationInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationDb")
            ?? throw new InvalidOperationException("Connection string 'NotificationDb' is not configured.");

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(NotificationDbContext).Assembly.FullName)));

        services.AddScoped<IInboxMessageStore, NotificationInboxMessageStore>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        // Note: RoundCreatedEvent consumer is registered in Phase 5. For now
        // the service exposes only the read API; no integration consumers
        // are wired so the message bus is connected but idle for this DB.

        return services;
    }
}
