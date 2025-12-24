using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce.API.Domain.Exceptions;

namespace E_Commerce.API.Domain.Entities;

/// <summary>
/// Shopping cart entity that holds products before order creation
/// </summary>
public class Cart : BaseEntity
{
    /// <summary>
    /// User identifier (simple string since we're skipping authentication)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Expiration timestamp for cart cleanup
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Collection of items in the cart
    /// </summary>
    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    /// <summary>
    /// Computed total amount for all items in cart
    /// </summary>
    [NotMapped]
    public decimal TotalAmount => Items?.Sum(item => item.SubTotal) ?? 0m;

    /// <summary>
    /// Total number of items in cart
    /// </summary>
    [NotMapped]
    public int TotalItems => Items?.Sum(item => item.Quantity) ?? 0;

    /// <summary>
    /// Check if cart has expired
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Extend cart expiration by specified hours (default 24)
    /// </summary>
    public void ExtendExpiration(int hours = 24)
    {
        ExpiresAt = DateTime.UtcNow.AddHours(hours);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add item to cart or update quantity if already exists
    /// </summary>
    public void AddOrUpdateItem(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        if (!product.IsActive)
            throw new DomainException($"Product {product.SKU} is not active");

        if (!product.HasSufficientStock(quantity))
            throw new InsufficientStockException(
                $"Product {product.SKU} has insufficient stock. Requested: {quantity}, Available: {product.StockQuantity}");

        var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);

        if (existingItem != null)
        {
            // Update existing item
            var newQuantity = existingItem.Quantity + quantity;

            if (!product.HasSufficientStock(newQuantity))
                throw new InsufficientStockException(
                    $"Product {product.SKU} has insufficient stock. Requested: {newQuantity}, Available: {product.StockQuantity}");

            existingItem.UpdateQuantity(newQuantity);
        }
        else
        {
            // Add new item - Don't set Id, let EF Core handle it
            var cartItem = new CartItem
            {
                Id = Guid.Empty, // Reset to empty to signal EF Core this is new
                CartId = Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price, // Snapshot current price
                CreatedAt = DateTime.UtcNow
            };
            Items.Add(cartItem);
        }

        ExtendExpiration(); // Extend cart expiration on activity
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    public void RemoveItem(Guid productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            Items.Remove(item);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Update item quantity in cart
    /// </summary>
    public void UpdateItemQuantity(Guid productId, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new DomainException($"Product not found in cart");

        item.UpdateQuantity(quantity);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clear all items from cart
    /// </summary>
    public void Clear()
    {
        Items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }
}
