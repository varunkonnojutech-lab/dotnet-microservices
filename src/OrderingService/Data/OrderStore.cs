using ECommerce.OrderingService.Models;

namespace ECommerce.OrderingService.Data;

/// <summary>
/// In-memory order store. Thread-safe via a private lock.
/// In a real service this would be backed by a database.
/// </summary>
public static class OrderStore
{
    private static readonly object _lock = new();

    public static readonly List<Order> Orders = new();

    public static Order Add(Order order)
    {
        lock (_lock)
        {
            Orders.Add(order);
        }

        return order;
    }

    public static IReadOnlyList<Order> GetAll()
    {
        lock (_lock)
        {
            return Orders.ToList();
        }
    }
}
