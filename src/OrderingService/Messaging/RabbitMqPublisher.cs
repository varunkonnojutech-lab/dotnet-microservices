using System.Text.Json;
using ECommerce.Contracts.Events;
using RabbitMQ.Client;

namespace ECommerce.OrderingService.Messaging;

/// <summary>
/// Singleton publisher that sends OrderCreatedEvent messages to RabbitMQ.
/// Connects lazily on first publish.
/// </summary>
public sealed class RabbitMqPublisher : IDisposable
{
    private const string QueueName = "order-created";

    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConfiguration _configuration;
    private readonly object _sync = new();

    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed;

    public RabbitMqPublisher(ILogger<RabbitMqPublisher> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private void EnsureConnected()
    {
        if (_channel is { IsOpen: true })
            return;

        lock (_sync)
        {
            if (_channel is { IsOpen: true })
                return;

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:Host"] ?? "localhost"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            _logger.LogInformation("Connected to RabbitMQ and declared queue '{Queue}'", QueueName);
        }
    }

    public void PublishOrderCreated(OrderCreatedEvent evt)
    {
        EnsureConnected();

        var body = JsonSerializer.SerializeToUtf8Bytes(evt);
        _channel!.BasicPublish(exchange: "", routingKey: QueueName, basicProperties: null, body: body);

        _logger.LogInformation("Published OrderCreatedEvent for order {OrderId}", evt.OrderId);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try { _channel?.Close(); } catch { /* shutting down */ }
        _channel?.Dispose();
        try { _connection?.Close(); } catch { /* shutting down */ }
        _connection?.Dispose();
    }
}
