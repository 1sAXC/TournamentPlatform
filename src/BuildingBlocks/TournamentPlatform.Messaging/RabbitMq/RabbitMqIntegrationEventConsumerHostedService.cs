using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Abstractions;

namespace TournamentPlatform.Messaging.RabbitMq;

public sealed class RabbitMqIntegrationEventConsumerHostedService<TEvent, TConsumer>(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqIntegrationEventConsumerHostedService<TEvent, TConsumer>> logger,
    string queueName) : BackgroundService
    where TEvent : IntegrationEvent
    where TConsumer : class, IIntegrationEventConsumer<TEvent>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilCancelledAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "RabbitMQ consumer {QueueName} failed. Reconnecting.", queueName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeUntilCancelledAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        DeclareTopology(channel);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) => await HandleDeliveryAsync(channel, args, cancellationToken);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        channel.BasicConsume(queueName, autoAck: false, consumer);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(_options.DeadLetterExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        var queueArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.DeadLetterExchangeName
        };

        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArguments);
        channel.QueueBind(queueName, _options.ExchangeName, typeof(TEvent).Name);

        var errorQueue = $"{queueName}.error";
        channel.QueueDeclare(errorQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(errorQueue, _options.DeadLetterExchangeName, "#");
    }

    private async Task HandleDeliveryAsync(IModel channel, BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(args.Body.Span);
            var integrationEvent = JsonSerializer.Deserialize<TEvent>(payload, JsonSerializerOptions)
                ?? throw new InvalidOperationException($"Cannot deserialize {typeof(TEvent).Name}.");

            var consumerName = typeof(TConsumer).FullName ?? typeof(TConsumer).Name;
            using var scope = serviceScopeFactory.CreateScope();
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxMessageStore>();

            if (await inbox.HasProcessedAsync(integrationEvent.EventId, consumerName, cancellationToken))
            {
                channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
            await consumer.ConsumeAsync(integrationEvent, cancellationToken);
            await inbox.MarkProcessedAsync(integrationEvent.EventId, consumerName, DateTime.UtcNow, cancellationToken);

            channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "RabbitMQ consumer {QueueName} failed to process delivery.", queueName);
            channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }
    }
}
