namespace ECommerce.Contracts.Events;

/// <summary>
/// Published by the Ordering service when an order is placed.
/// Consumed by the Catalog service to decrement product stock.
/// </summary>
public sealed record OrderCreatedEvent(
    Guid OrderId,
    int ProductId,
    int Quantity,
    DateTimeOffset OccurredAt);
