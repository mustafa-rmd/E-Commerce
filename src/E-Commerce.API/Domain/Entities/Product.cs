using E_Commerce.API.Domain.Exceptions;

namespace E_Commerce.API.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    // Stock management methods
    public bool HasSufficientStock(int quantity)
    {
        return StockQuantity >= quantity && IsActive;
    }

    public void ReserveStock(int quantity)
    {
        if (!IsActive)
        {
            throw new DomainException($"Product {SKU} is not active");
        }

        if (!HasSufficientStock(quantity))
        {
            throw new InsufficientStockException(
                $"Product {SKU} has insufficient stock. Requested: {quantity}, Available: {StockQuantity}");
        }

        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseStock(int quantity)
    {
        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainException("Stock quantity cannot be negative");
        }

        StockQuantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
