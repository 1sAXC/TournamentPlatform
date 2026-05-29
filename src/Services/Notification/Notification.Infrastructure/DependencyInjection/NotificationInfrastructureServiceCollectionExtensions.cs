using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Notifications.Abstractions;
using Notification.Application.Notifications.Services;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Abstractions;
using TournamentPlatform.Messaging.DependencyInjection;

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
        services.AddScoped<IRoundCreatedFanout, RoundCreatedFanout>();

        // RabbitMQ consumers
        services.AddRabbitMqConsumer<RoundCreatedEvent, NotificationRoundCreatedConsumer>("notification.round-created");

        return services;
    }
}
