using RabbitMQ.Client;

namespace TournamentPlatform.Messaging.RabbitMq;

public interface IRabbitMqConnectionProvider : IDisposable
{
    IConnection GetConnection();
}
