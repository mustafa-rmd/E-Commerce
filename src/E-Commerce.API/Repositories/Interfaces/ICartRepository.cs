using E_Commerce.API.Domain.Entities;

namespace E_Commerce.API.Repositories.Interfaces;

public interface ICartRepository : IGenericRepository<Cart>
{
    /// <summary>
    /// Get cart by user ID with all items and product details
    /// </summary>
    Task<Cart?> GetByUserIdAsync(string userId);

    /// <summary>
    /// Get all expired carts for cleanup
    /// </summary>
    Task<IEnumerable<Cart>> GetExpiredCartsAsync();

    /// <summary>
    /// Get cart with items for a specific user
    /// </summary>
    Task<Cart?> GetCartWithItemsAsync(string userId);

    /// <summary>
    /// Check if user has an active cart
    /// </summary>
    Task<bool> HasActiveCartAsync(string userId);

    /// <summary>
    /// Delete expired carts
    /// </summary>
    Task<int> DeleteExpiredCartsAsync();
}
