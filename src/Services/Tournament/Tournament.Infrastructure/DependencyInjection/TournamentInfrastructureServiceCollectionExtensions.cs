using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Infrastructure.Messaging;
using Tournament.Infrastructure.Persistence;
using Tournament.Infrastructure.Persistence.Repositories;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Abstractions;
using TournamentPlatform.Messaging.DependencyInjection;
using TournamentPlatform.Messaging.Outbox;

namespace Tournament.Infrastructure.DependencyInjection;

public static class TournamentInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTournamentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TournamentDb")
            ?? throw new InvalidOperationException("Connection string 'TournamentDb' is not configured.");

        services.AddDbContext<TournamentDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(TournamentDbContext).Assembly.FullName)));

        services.AddScoped<IInboxMessageStore, TournamentInboxMessageStore>();
        services.AddScoped<IOutboxMessageStore, TournamentOutboxMessageStore>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<ITournamentRepository, TournamentRepository>();
        services.AddScoped<IPlayerRatingProjectionRepository, PlayerRatingProjectionRepository>();
        services.AddScoped<IUserProjectionRepository, UserProjectionRepository>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        services.AddRabbitMqConsumer<UserCreatedEvent, TournamentUserCreatedConsumer>("tournament.user-created");
        services.AddRabbitMqConsumer<RatingUpdatedEvent, TournamentRatingUpdatedConsumer>("tournament.rating-updated");
        services.AddRabbitMqConsumer<UserDeletedEvent, TournamentUserDeletedConsumer>("tournament.user-deleted");

        return services;
    }
}
