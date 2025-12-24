using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce.API.Domain.Exceptions;

namespace E_Commerce.API.Domain.Entities;

/// <summary>
/// Individual item within a shopping cart
/// </summary>
public class CartItem : BaseEntity
{
    /// <summary>
    /// Foreign key to Cart
    /// </summary>
    public Guid CartId { get; set; }

    /// <summary>
    /// Navigation property to Cart
    /// </summary>
    public virtual Cart Cart { get; set; } = null!;

    /// <summary>
    /// Foreign key to Product
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property to Product
    /// </summary>
    public virtual Product Product { get; set; } = null!;

    /// <summary>
    /// Quantity of this product in cart
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit price snapshot at time of adding to cart
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Computed subtotal for this item
    /// </summary>
    [NotMapped]
    public decimal SubTotal => Quantity * UnitPrice;

    /// <summary>
    /// Update quantity with validation
    /// </summary>
    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        Quantity = newQuantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update unit price (useful when cart is refreshed)
    /// </summary>
    public void UpdateUnitPrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new DomainException("Unit price must be greater than zero");

        UnitPrice = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }
}
