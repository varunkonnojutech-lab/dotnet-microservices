using ECommerce.CatalogService.Models;

namespace ECommerce.CatalogService.Data;

/// <summary>
/// In-memory product inventory. Thread-safe via a private lock.
/// In a real service this would be backed by a database.
/// </summary>
public static class ProductStore
{
    private static readonly object _lock = new();

    public static readonly Dictionary<int, Product> Products = new()
    {
        [1] = new Product { Id = 1, Name = "Laptop", Price = 999.99m, Stock = 10 },
        [2] = new Product { Id = 2, Name = "Wireless Mouse", Price = 29.99m, Stock = 50 },
        [3] = new Product { Id = 3, Name = "Mechanical Keyboard", Price = 79.99m, Stock = 30 },
        [4] = new Product { Id = 4, Name = "4K Monitor", Price = 249.99m, Stock = 15 }
    };

    /// <summary>
    /// Decrements stock for a product, floored at zero.
    /// Returns false when the product does not exist.
    /// </summary>
    public static bool TryDecrementStock(int productId, int quantity, out int previousStock, out int newStock)
    {
        lock (_lock)
        {
            if (!Products.TryGetValue(productId, out var product))
            {
                previousStock = 0;
                newStock = 0;
                return false;
            }

            previousStock = product.Stock;
            product.Stock = Math.Max(0, product.Stock - quantity);
            newStock = product.Stock;
            return true;
        }
    }

    public static bool TrySetStock(int productId, int quantity)
    {
        lock (_lock)
        {
            if (!Products.TryGetValue(productId, out var product))
                return false;

            product.Stock = quantity;
            return true;
        }
    }
}
