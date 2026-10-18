using ECommerce.Contracts.Events;
using ECommerce.OrderingService.Data;
using ECommerce.OrderingService.Messaging;
using ECommerce.OrderingService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RabbitMqPublisher>();

var app = builder.Build();

app.MapPost("/orders", (CreateOrderRequest req, RabbitMqPublisher publisher, ILogger<Program> logger) =>
{
    if (req.ProductId <= 0 || req.Quantity <= 0)
        return Results.BadRequest("ProductId and Quantity must be greater than zero.");

    var order = new Order
    {
        Id = Guid.NewGuid(),
        ProductId = req.ProductId,
        Quantity = req.Quantity,
        Status = "Created",
        CreatedAt = DateTimeOffset.UtcNow
    };

    OrderStore.Add(order);

    publisher.PublishOrderCreated(new OrderCreatedEvent(order.Id, order.ProductId, order.Quantity, order.CreatedAt));

    logger.LogInformation("Created order {OrderId} for product {ProductId} x{Quantity}", order.Id, order.ProductId, order.Quantity);

    return Results.Created($"/orders/{order.Id}", order);
});

app.MapGet("/orders", () => Results.Ok(OrderStore.GetAll()));

app.MapGet("/orders/{id:guid}", (Guid id) =>
{
    var order = OrderStore.GetAll().FirstOrDefault(o => o.Id == id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

app.Run();
