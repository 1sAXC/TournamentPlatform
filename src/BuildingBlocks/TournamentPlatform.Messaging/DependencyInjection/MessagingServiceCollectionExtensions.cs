using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Abstractions;
using TournamentPlatform.Messaging.RabbitMq;

namespace TournamentPlatform.Messaging.DependencyInjection;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
        return services;
    }

    public static IServiceCollection AddRabbitMqConsumer<TEvent, TConsumer>(
        this IServiceCollection services,
        string queueName)
        where TEvent : IntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
    {
        services.AddScoped<TConsumer>();
        services.AddSingleton<IHostedService>(serviceProvider =>
            new RabbitMqIntegrationEventConsumerHostedService<TEvent, TConsumer>(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMqIntegrationEventConsumerHostedService<TEvent, TConsumer>>>(),
                queueName));

        return services;
    }
}
