using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Domain.Enums;

namespace E_Commerce.API.Repositories.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    /// <summary>
    /// Get order by order number with all items
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber);

    /// <summary>
    /// Get all orders for a specific user
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId);

    /// <summary>
    /// Get orders by status
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status);

    /// <summary>
    /// Get order with items for a specific user
    /// </summary>
    Task<Order?> GetOrderWithItemsAsync(Guid orderId);

    /// <summary>
    /// Get user orders with a specific status
    /// </summary>
    Task<IEnumerable<Order>> GetUserOrdersByStatusAsync(string userId, OrderStatus status);
}
