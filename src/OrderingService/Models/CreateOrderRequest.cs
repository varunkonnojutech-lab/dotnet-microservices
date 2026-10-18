namespace ECommerce.OrderingService.Models;

public sealed record CreateOrderRequest(int ProductId, int Quantity);
