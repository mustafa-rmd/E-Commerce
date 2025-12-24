using E_Commerce.API.Domain.Enums;
using E_Commerce.API.DTOs;

namespace E_Commerce.API.Services.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Get order by ID
    /// </summary>
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId);

    /// <summary>
    /// Get order by order number
    /// </summary>
    Task<OrderDto?> GetOrderByNumberAsync(string orderNumber);

    /// <summary>
    /// Get all orders for a user
    /// </summary>
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);

    /// <summary>
    /// Get user orders by status
    /// </summary>
    Task<IEnumerable<OrderDto>> GetUserOrdersByStatusAsync(string userId, OrderStatus status);

    /// <summary>
    /// Create order from cart with stock validation and price snapshots
    /// </summary>
    Task<OrderDto> CreateOrderFromCartAsync(CreateOrderFromCartDto createDto);

    /// <summary>
    /// Create order directly with stock validation and price snapshots
    /// </summary>
    Task<OrderDto> CreateOrderAsync(CreateOrderDto createDto);

    /// <summary>
    /// Confirm order
    /// </summary>
    Task<OrderDto> ConfirmOrderAsync(Guid orderId);

    /// <summary>
    /// Mark order as paid
    /// </summary>
    Task<OrderDto> MarkOrderAsPaidAsync(Guid orderId);

    /// <summary>
    /// Ship order
    /// </summary>
    Task<OrderDto> ShipOrderAsync(Guid orderId);

    /// <summary>
    /// Deliver order
    /// </summary>
    Task<OrderDto> DeliverOrderAsync(Guid orderId);

    /// <summary>
    /// Cancel order and release reserved stock
    /// </summary>
    Task<OrderDto> CancelOrderAsync(Guid orderId, CancelOrderDto cancelDto);
}
