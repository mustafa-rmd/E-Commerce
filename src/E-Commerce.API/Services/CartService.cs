using E_Commerce.API.Data;
using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Domain.Exceptions;
using E_Commerce.API.DTOs;
using E_Commerce.API.Repositories.Interfaces;
using E_Commerce.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CartService> _logger;

    public CartService(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, ILogger<CartService> logger)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CartDto?> GetCartByUserIdAsync(string userId)
    {
        _logger.LogInformation("Getting cart for user: {UserId}", userId);

        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(userId);

        if (cart == null)
        {
            _logger.LogInformation("No cart found for user: {UserId}", userId);
            return null;
        }

        if (cart.IsExpired())
        {
            _logger.LogInformation("Cart expired for user: {UserId}, deleting", userId);
            await _unitOfWork.Carts.DeleteAsync(cart);
            await _unitOfWork.SaveChangesAsync();
            return null;
        }

        return MapToDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(string userId, AddToCartDto addToCartDto)
    {
        _logger.LogInformation("Adding product {ProductId} to cart for user: {UserId}", addToCartDto.ProductId, userId);

        // Get or create cart
        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(userId);

        if (cart == null)
        {
            _logger.LogInformation("Creating new cart for user: {UserId}", userId);
            cart = new Cart
            {
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
            await _unitOfWork.Carts.AddAsync(cart);
        }
        else if (cart.IsExpired())
        {
            _logger.LogInformation("Cart expired, resetting for user: {UserId}", userId);
            cart.Clear();
            cart.ExtendExpiration();
        }

        // Get product
        var product = await _unitOfWork.Products.GetByIdAsync(addToCartDto.ProductId);
        if (product == null)
        {
            throw new DomainException($"Product with ID {addToCartDto.ProductId} not found");
        }

        // Count items before adding
        var itemCountBefore = cart.Items.Count;

        // Add or update item
        cart.AddOrUpdateItem(product, addToCartDto.Quantity);

        // If a new item was added, mark it as Added for EF Core
        if (cart.Items.Count > itemCountBefore)
        {
            var newItem = cart.Items.FirstOrDefault(i => i.Id == Guid.Empty);
            if (newItem != null)
            {
                newItem.Product = product; // Set navigation property
                _dbContext.Entry(newItem).State = EntityState.Added;
            }
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} added to cart for user: {UserId}", addToCartDto.ProductId, userId);

        return MapToDto(cart);
    }

    public async Task<CartDto> UpdateCartItemAsync(string userId, Guid productId, UpdateCartItemDto updateDto)
    {
        _logger.LogInformation("Updating cart item {ProductId} for user: {UserId}", productId, userId);

        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(userId);
        if (cart == null)
        {
            throw new DomainException($"Cart not found for user {userId}");
        }

        if (cart.IsExpired())
        {
            throw new DomainException("Cart has expired");
        }

        // Get product to validate stock
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            throw new DomainException($"Product with ID {productId} not found");
        }

        if (!product.HasSufficientStock(updateDto.Quantity))
        {
            throw new InsufficientStockException(
                $"Product {product.SKU} has insufficient stock. Requested: {updateDto.Quantity}, Available: {product.StockQuantity}");
        }

        cart.UpdateItemQuantity(productId, updateDto.Quantity);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Cart item {ProductId} updated for user: {UserId}", productId, userId);

        return MapToDto(cart);
    }

    public async Task<CartDto> RemoveCartItemAsync(string userId, Guid productId)
    {
        _logger.LogInformation("Removing cart item {ProductId} for user: {UserId}", productId, userId);

        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(userId);
        if (cart == null)
        {
            throw new DomainException($"Cart not found for user {userId}");
        }

        cart.RemoveItem(productId);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Cart item {ProductId} removed for user: {UserId}", productId, userId);

        return MapToDto(cart);
    }

    public async Task<CartDto> ClearCartAsync(string userId)
    {
        _logger.LogInformation("Clearing cart for user: {UserId}", userId);

        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(userId);
        if (cart == null)
        {
            throw new DomainException($"Cart not found for user {userId}");
        }

        cart.Clear();

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Cart cleared for user: {UserId}", userId);

        return MapToDto(cart);
    }

    public async Task<bool> DeleteCartAsync(string userId)
    {
        _logger.LogInformation("Deleting cart for user: {UserId}", userId);

        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(userId);
        if (cart == null)
        {
            return false;
        }

        await _unitOfWork.Carts.DeleteAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Cart deleted for user: {UserId}", userId);

        return true;
    }

    private CartDto MapToDto(Cart cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            ExpiresAt = cart.ExpiresAt,
            TotalAmount = cart.TotalAmount,
            TotalItems = cart.TotalItems,
            CreatedAt = cart.CreatedAt,
            UpdatedAt = cart.UpdatedAt,
            Items = cart.Items.Select(item => new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                ProductSKU = item.Product.SKU,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal
            }).ToList()
        };
    }
}
