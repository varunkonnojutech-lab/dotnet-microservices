using System.Text.Json;
using ECommerce.CatalogService.Data;
using ECommerce.Contracts.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.CatalogService.Messaging;

/// <summary>
/// Background consumer that listens for OrderCreatedEvent messages and
/// decrements the corresponding product stock in the Catalog service.
/// </summary>
public sealed class RabbitMqConsumer : BackgroundService
{
    private const string QueueName = "order-created";

    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumer(ILogger<RabbitMqConsumer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"] ?? "localhost",
            DispatchConsumersAsync = true
        };

        // Retry connecting until RabbitMQ is reachable (e.g. it starts after us in compose).
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                _logger.LogInformation("Connected to RabbitMQ and declared queue '{Queue}'", QueueName);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (_channel is null || stoppingToken.IsCancellationRequested)
            return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (sender, ea) =>
        {
            try
            {
                var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(ea.Body.ToArray());

                if (evt is null)
                {
                    _logger.LogWarning("Received empty OrderCreatedEvent payload; discarding");
                }
                else if (ProductStore.TryDecrementStock(evt.ProductId, evt.Quantity, out var previousStock, out var newStock))
                {
                    if (previousStock < evt.Quantity)
                    {
                        _logger.LogWarning(
                            "Insufficient stock for product {ProductId}: had {PreviousStock}, ordered {Quantity}",
                            evt.ProductId, previousStock, evt.Quantity);
                    }

                    _logger.LogInformation(
                        "Order {OrderId}: decremented stock for product {ProductId} to {Stock}",
                        evt.OrderId, evt.ProductId, newStock);
                }
                else
                {
                    _logger.LogWarning(
                        "Product {ProductId} not found; ignoring OrderCreatedEvent {OrderId}",
                        evt.ProductId, evt.OrderId);
                }

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order-created message; requeueing");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }

            await Task.CompletedTask;
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        // Keep the background service alive until shutdown is requested.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        try { _channel?.Close(); } catch { /* shutting down */ }
        _channel?.Dispose();
        try { _connection?.Close(); } catch { /* shutting down */ }
        _connection?.Dispose();
        base.Dispose();
    }
}
