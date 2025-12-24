using E_Commerce.API.DTOs;
using E_Commerce.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartsController> _logger;

    public CartsController(ICartService cartService, ILogger<CartsController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Get cart for a specific user
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<CartDto>> GetCart(string userId)
    {
        _logger.LogInformation("GET /api/carts/{UserId}", userId);

        var cart = await _cartService.GetCartByUserIdAsync(userId);
        if (cart == null)
        {
            _logger.LogInformation("Cart not found for user: {UserId}", userId);
            return NotFound(new { message = $"Cart not found for user {userId}" });
        }

        return Ok(cart);
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    [HttpPost("{userId}/items")]
    public async Task<ActionResult<CartDto>> AddToCart(string userId, [FromBody] AddToCartDto addToCartDto)
    {
        _logger.LogInformation("POST /api/carts/{UserId}/items - Product: {ProductId}, Quantity: {Quantity}",
            userId, addToCartDto.ProductId, addToCartDto.Quantity);

        try
        {
            var cart = await _cartService.AddToCartAsync(userId, addToCartDto);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    [HttpPut("{userId}/items/{productId:guid}")]
    public async Task<ActionResult<CartDto>> UpdateCartItem(
        string userId,
        Guid productId,
        [FromBody] UpdateCartItemDto updateDto)
    {
        _logger.LogInformation("PUT /api/carts/{UserId}/items/{ProductId} - Quantity: {Quantity}",
            userId, productId, updateDto.Quantity);

        try
        {
            var cart = await _cartService.UpdateCartItemAsync(userId, productId, updateDto);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    [HttpDelete("{userId}/items/{productId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveCartItem(string userId, Guid productId)
    {
        _logger.LogInformation("DELETE /api/carts/{UserId}/items/{ProductId}", userId, productId);

        try
        {
            var cart = await _cartService.RemoveCartItemAsync(userId, productId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Clear all items from cart
    /// </summary>
    [HttpDelete("{userId}/items")]
    public async Task<ActionResult<CartDto>> ClearCart(string userId)
    {
        _logger.LogInformation("DELETE /api/carts/{UserId}/items", userId);

        try
        {
            var cart = await _cartService.ClearCartAsync(userId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete cart
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<ActionResult> DeleteCart(string userId)
    {
        _logger.LogInformation("DELETE /api/carts/{UserId}", userId);

        var deleted = await _cartService.DeleteCartAsync(userId);
        if (!deleted)
        {
            _logger.LogWarning("Cart not found for user: {UserId}", userId);
            return NotFound(new { message = $"Cart not found for user {userId}" });
        }

        return NoContent();
    }
}
