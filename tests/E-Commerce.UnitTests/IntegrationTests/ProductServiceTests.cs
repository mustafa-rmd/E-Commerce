using E_Commerce.API.Data;
using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Domain.Exceptions;
using E_Commerce.API.DTOs;
using E_Commerce.API.Repositories;
using E_Commerce.API.Services;
using E_Commerce.UnitTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace E_Commerce.UnitTests.IntegrationTests;

[Collection("Database")]
public class ProductServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private ApplicationDbContext _dbContext = null!;
    private ProductService _productService = null!;

    public ProductServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _dbContext = await _fixture.CreateCleanDbContextAsync();
        var unitOfWork = new UnitOfWork(_dbContext);
        var logger = new Mock<ILogger<ProductService>>().Object;
        _productService = new ProductService(unitOfWork, logger);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CreateProduct_ShouldCreateProductSuccessfully()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            SKU = "TEST-SKU-001",
            StockQuantity = 100
        };

        // Act
        var result = await _productService.CreateProductAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);
        result.SKU.Should().Be("TEST-SKU-001");
        result.StockQuantity.Should().Be(100);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductById_ShouldReturnProduct_WhenExists()
    {
        // Arrange
        var product = new Product
        {
            Name = "Existing Product",
            Description = "Description",
            Price = 50.00m,
            SKU = "EXIST-001",
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _productService.GetProductByIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be("Existing Product");
    }

    [Fact]
    public async Task GetProductById_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _productService.GetProductByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStock_ShouldUpdateSuccessfully()
    {
        // Arrange
        var product = new Product
        {
            Name = "Stock Test Product",
            Description = "Description",
            Price = 25.00m,
            SKU = "STOCK-001",
            StockQuantity = 50,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _productService.UpdateStockAsync(product.Id, 100);

        // Assert
        result.Should().NotBeNull();
        result.StockQuantity.Should().Be(100);
    }

    [Fact]
    public async Task DeleteProduct_ShouldSoftDelete()
    {
        // Arrange
        var product = new Product
        {
            Name = "Delete Test Product",
            Description = "Description",
            Price = 30.00m,
            SKU = "DELETE-001",
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Act
        await _productService.DeleteProductAsync(product.Id);

        // Assert
        var deletedProduct = await _productService.GetProductByIdAsync(product.Id);
        deletedProduct.Should().NotBeNull();
        deletedProduct!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllProducts_ShouldReturnAllProducts()
    {
        // Arrange
        var activeProduct = new Product
        {
            Name = "Active Product",
            Description = "Description",
            Price = 10.00m,
            SKU = "ACTIVE-001",
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inactiveProduct = new Product
        {
            Name = "Inactive Product",
            Description = "Description",
            Price = 20.00m,
            SKU = "INACTIVE-001",
            StockQuantity = 5,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.AddRange(activeProduct, inactiveProduct);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.SKU == "ACTIVE-001");
        result.Should().Contain(p => p.SKU == "INACTIVE-001");
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}