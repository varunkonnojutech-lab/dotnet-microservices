using ECommerce.CatalogService.Data;
using ECommerce.CatalogService.Messaging;
using ECommerce.CatalogService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<RabbitMqConsumer>();

var app = builder.Build();

app.MapGet("/products", () => Results.Ok(ProductStore.Products.Values));

app.MapGet("/products/{id:int}", (int id) =>
    ProductStore.Products.TryGetValue(id, out var product)
        ? Results.Ok(product)
        : Results.NotFound());

app.MapPut("/products/{id:int}/stock", (int id, UpdateStockRequest req) =>
    ProductStore.TrySetStock(id, req.Quantity)
        ? Results.Ok(ProductStore.Products[id])
        : Results.NotFound());

app.Run();
