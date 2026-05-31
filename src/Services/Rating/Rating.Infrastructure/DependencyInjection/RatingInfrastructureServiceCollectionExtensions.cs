using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rating.Application.Ratings.Abstractions;
using Rating.Application.Ratings.Services;
using Rating.Infrastructure.Messaging;
using Rating.Infrastructure.Persistence;
using Rating.Infrastructure.Persistence.Repositories;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Abstractions;
using TournamentPlatform.Messaging.DependencyInjection;
using TournamentPlatform.Messaging.Outbox;

namespace Rating.Infrastructure.DependencyInjection;

public static class RatingInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRatingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RatingDb")
            ?? throw new InvalidOperationException("Connection string 'RatingDb' is not configured.");

        services.AddDbContext<RatingDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(RatingDbContext).Assembly.FullName)));

        services.AddScoped<IInboxMessageStore, RatingInboxMessageStore>();
        services.AddScoped<IOutboxMessageStore, RatingOutboxMessageStore>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IEloCalculator, EloCalculator>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        services.AddRabbitMqConsumer<UserCreatedEvent, RatingUserCreatedConsumer>("rating.user-created");
        services.AddRabbitMqConsumer<UserBlockedEvent, RatingUserBlockedConsumer>("rating.user-blocked");
        services.AddRabbitMqConsumer<UserRoleChangedEvent, RatingUserRoleChangedConsumer>("rating.user-role-changed");
        services.AddRabbitMqConsumer<MatchCompletedEvent, RatingMatchCompletedConsumer>("rating.match-completed");
        services.AddRabbitMqConsumer<TournamentCompletedEvent, RatingTournamentCompletedConsumer>("rating.tournament-completed");

        return services;
    }
}
