using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce.API.Domain.Enums;
using E_Commerce.API.Domain.Exceptions;

namespace E_Commerce.API.Domain.Entities;

/// <summary>
/// Order entity with state machine for order lifecycle
/// </summary>
public class Order : BaseEntity
{
    /// <summary>
    /// Unique order number for external reference
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// User identifier (simple string since we're skipping authentication)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Current order status
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>
    /// Total amount for all items
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Shipping cost
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Tax amount
    /// </summary>
    public decimal Tax { get; set; }

    /// <summary>
    /// Grand total (TotalAmount + ShippingCost + Tax)
    /// </summary>
    [NotMapped]
    public decimal GrandTotal => TotalAmount + ShippingCost + Tax;

    /// <summary>
    /// Collection of order items
    /// </summary>
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    // Status transition timestamps
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation (if cancelled)
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Confirm the order
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOrderStateTransitionException(
                $"Cannot transition from {Status} to {OrderStatus.Confirmed}. Order must be in Pending status.");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark order as paid
    /// </summary>
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOrderStateTransitionException(
                $"Cannot transition from {Status} to {OrderStatus.Paid}. Order must be in Confirmed status.");

        Status = OrderStatus.Paid;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark order as shipped
    /// </summary>
    public void Ship()
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOrderStateTransitionException(
                $"Cannot transition from {Status} to {OrderStatus.Shipped}. Order must be in Paid status.");

        Status = OrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark order as delivered
    /// </summary>
    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOrderStateTransitionException(
                $"Cannot transition from {Status} to {OrderStatus.Delivered}. Order must be in Shipped status.");

        Status = OrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancel the order
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Delivered)
            throw new InvalidOrderStateTransitionException(
                $"Cannot cancel order in {Status} status. Delivered orders cannot be cancelled.");

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if order can be cancelled
    /// </summary>
    public bool CanBeCancelled() => Status != OrderStatus.Delivered && Status != OrderStatus.Cancelled;

    /// <summary>
    /// Generate a unique order number
    /// </summary>
    public static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"ORD-{timestamp}-{random}";
    }
}
