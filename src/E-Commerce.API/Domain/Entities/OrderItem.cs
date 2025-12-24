using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.API.Domain.Entities;

/// <summary>
/// Individual item within an order with price snapshot
/// </summary>
public class OrderItem : BaseEntity
{
    /// <summary>
    /// Foreign key to Order
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Navigation property to Order
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Foreign key to Product
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property to Product
    /// </summary>
    public virtual Product Product { get; set; } = null!;

    /// <summary>
    /// Product name snapshot at order time (for historical reference)
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product SKU snapshot at order time (for historical reference)
    /// </summary>
    public string ProductSKU { get; set; } = string.Empty;

    /// <summary>
    /// Quantity ordered
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit price snapshot at order time
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Computed subtotal for this item
    /// </summary>
    [NotMapped]
    public decimal SubTotal => Quantity * UnitPrice;

    /// <summary>
    /// Create OrderItem from Product with price snapshot
    /// </summary>
    public static OrderItem FromProduct(Product product, int quantity)
    {
        return new OrderItem
        {
            Id = Guid.Empty, // Let EF Core generate
            ProductId = product.Id,
            ProductName = product.Name,
            ProductSKU = product.SKU,
            Quantity = quantity,
            UnitPrice = product.Price, // Price snapshot
            CreatedAt = DateTime.UtcNow
        };
    }
}
