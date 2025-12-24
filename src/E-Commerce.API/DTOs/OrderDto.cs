using E_Commerce.API.Domain.Enums;

namespace E_Commerce.API.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class CreateOrderDto
{
    public string UserId { get; set; } = string.Empty;
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public decimal ShippingCost { get; set; } = 0m;
    public decimal Tax { get; set; } = 0m;
}

public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderFromCartDto
{
    public string UserId { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; } = 0m;
    public decimal Tax { get; set; } = 0m;
}

public class CancelOrderDto
{
    public string Reason { get; set; } = string.Empty;
}
