using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Domain.Exceptions;
using E_Commerce.API.DTOs;
using E_Commerce.API.Repositories.Interfaces;
using E_Commerce.API.Services.Interfaces;

namespace E_Commerce.API.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        _logger.LogInformation("Getting all products");
        var products = await _unitOfWork.Products.GetAllAsync();
        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> GetActiveProductsAsync()
    {
        _logger.LogInformation("Getting active products");
        var products = await _unitOfWork.Products.GetActiveProductsAsync();
        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        _logger.LogInformation("Getting product by ID: {ProductId}", id);
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        return product != null ? MapToDto(product) : null;
    }

    public async Task<ProductDto?> GetProductBySkuAsync(string sku)
    {
        _logger.LogInformation("Getting product by SKU: {SKU}", sku);
        var product = await _unitOfWork.Products.GetBySkuAsync(sku);
        return product != null ? MapToDto(product) : null;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createDto)
    {
        _logger.LogInformation("Creating product with SKU: {SKU}", createDto.SKU);

        // Check if SKU already exists
        var existingProduct = await _unitOfWork.Products.GetBySkuAsync(createDto.SKU);
        if (existingProduct != null)
        {
            _logger.LogWarning("Product with SKU {SKU} already exists", createDto.SKU);
            throw new DomainException($"Product with SKU '{createDto.SKU}' already exists");
        }

        var product = new Product
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            SKU = createDto.SKU,
            StockQuantity = createDto.StockQuantity,
            IsActive = true
        };

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product created with ID: {ProductId}", product.Id);
        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto updateDto)
    {
        _logger.LogInformation("Updating product: {ProductId}", id);

        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            return null;
        }

        // Update only provided fields
        if (updateDto.Name != null) product.Name = updateDto.Name;
        if (updateDto.Description != null) product.Description = updateDto.Description;
        if (updateDto.Price.HasValue) product.Price = updateDto.Price.Value;
        if (updateDto.StockQuantity.HasValue) product.StockQuantity = updateDto.StockQuantity.Value;
        if (updateDto.IsActive.HasValue) product.IsActive = updateDto.IsActive.Value;

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product updated: {ProductId}", id);
        return MapToDto(product);
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        _logger.LogInformation("Deleting product: {ProductId}", id);

        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            return false;
        }

        // Soft delete - just mark as inactive
        product.IsActive = false;
        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product soft deleted: {ProductId}", id);
        return true;
    }

    public async Task<ProductDto?> UpdateStockAsync(Guid id, int quantity)
    {
        _logger.LogInformation("Updating stock for product: {ProductId} to {Quantity}", id, quantity);

        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            return null;
        }

        product.UpdateStock(quantity);
        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Stock updated for product: {ProductId}", id);
        return MapToDto(product);
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync(int threshold = 10)
    {
        _logger.LogInformation("Getting low stock products (threshold: {Threshold})", threshold);
        var products = await _unitOfWork.Products.GetLowStockProductsAsync(threshold);
        return products.Select(MapToDto);
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            SKU = product.SKU,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
