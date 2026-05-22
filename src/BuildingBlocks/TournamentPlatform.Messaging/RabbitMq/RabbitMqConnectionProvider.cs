using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TournamentPlatform.Messaging.RabbitMq;

public sealed class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options) : IRabbitMqConnectionProvider
{
    private readonly object _lock = new();
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;

    public IConnection GetConnection()
    {
        var connection = _connection;
        if (connection?.IsOpen == true)
        {
            return connection;
        }

        lock (_lock)
        {
            if (_connection?.IsOpen == true)
            {
                return _connection;
            }

            _connection?.Dispose();
            _connection = CreateConnectionFactory().CreateConnection();
            return _connection;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };
    }
}
