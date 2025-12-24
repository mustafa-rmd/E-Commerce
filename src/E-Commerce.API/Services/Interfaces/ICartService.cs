using E_Commerce.API.DTOs;

namespace E_Commerce.API.Services.Interfaces;

public interface ICartService
{
    /// <summary>
    /// Get cart for a specific user
    /// </summary>
    Task<CartDto?> GetCartByUserIdAsync(string userId);

    /// <summary>
    /// Add product to cart (or update quantity if already exists)
    /// </summary>
    Task<CartDto> AddToCartAsync(string userId, AddToCartDto addToCartDto);

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    Task<CartDto> UpdateCartItemAsync(string userId, Guid productId, UpdateCartItemDto updateDto);

    /// <summary>
    /// Remove item from cart
    /// </summary>
    Task<CartDto> RemoveCartItemAsync(string userId, Guid productId);

    /// <summary>
    /// Clear all items from cart
    /// </summary>
    Task<CartDto> ClearCartAsync(string userId);

    /// <summary>
    /// Delete cart
    /// </summary>
    Task<bool> DeleteCartAsync(string userId);
}
