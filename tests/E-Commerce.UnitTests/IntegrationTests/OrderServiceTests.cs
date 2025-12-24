using E_Commerce.API.Data;
using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Domain.Enums;
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
public class OrderServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private ApplicationDbContext _dbContext = null!;
    private OrderService _orderService = null!;

    public OrderServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _dbContext = await _fixture.CreateCleanDbContextAsync();
        var unitOfWork = new UnitOfWork(_dbContext);
        var logger = new Mock<ILogger<OrderService>>().Object;
        _orderService = new OrderService(unitOfWork, _dbContext, logger);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_ShouldReserveStock_AndCreatePriceSnapshots()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Laptop",
            Description = "High performance laptop",
            Price = 1299.99m,
            SKU = "LAPTOP-001",
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateOrderDto
        {
            UserId = "test-user-001",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 2 }
            },
            ShippingCost = 15.00m,
            Tax = 130.00m
        };

        // Act
        var result = await _orderService.CreateOrderAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Pending);
        result.TotalAmount.Should().Be(1299.99m * 2); // Price snapshot
        result.Items.Should().HaveCount(1);
        result.Items.First().UnitPrice.Should().Be(1299.99m); // Price snapshot
        result.Items.First().ProductName.Should().Be("Test Laptop"); // Name snapshot
        result.Items.First().ProductSKU.Should().Be("LAPTOP-001"); // SKU snapshot

        // Verify stock was reserved
        var updatedProduct = await _dbContext.Products.FindAsync(product.Id);
        updatedProduct!.StockQuantity.Should().Be(8); // 10 - 2
    }

    [Fact]
    public async Task CreateOrder_ShouldThrowException_WhenInsufficientStock()
    {
        // Arrange
        var product = new Product
        {
            Name = "Limited Stock Product",
            Description = "Description",
            Price = 50.00m,
            SKU = "LIMITED-001",
            StockQuantity = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateOrderDto
        {
            UserId = "test-user-001",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 5 } // More than available
            },
            ShippingCost = 10.00m,
            Tax = 5.00m
        };

        // Act & Assert
        var act = async () => await _orderService.CreateOrderAsync(createDto);
        await act.Should().ThrowAsync<InsufficientStockException>();

        // Verify stock was NOT reserved
        var productAfter = await _dbContext.Products.FindAsync(product.Id);
        productAfter!.StockQuantity.Should().Be(2); // Unchanged
    }

    [Fact]
    public async Task ConfirmOrder_ShouldUpdateStatus()
    {
        // Arrange
        var product = new Product
        {
            Name = "Confirm Test Product",
            Description = "Description",
            Price = 100.00m,
            SKU = "CONFIRM-001",
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var order = await _orderService.CreateOrderAsync(new CreateOrderDto
        {
            UserId = "test-user-002",
            Items = new List<CreateOrderItemDto> { new() { ProductId = product.Id, Quantity = 1 } },
            ShippingCost = 5.00m,
            Tax = 10.00m
        });

        // Act
        var result = await _orderService.ConfirmOrderAsync(order.Id);

        // Assert
        result.Status.Should().Be(OrderStatus.Confirmed);
        result.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelOrder_ShouldReleaseReservedStock()
    {
        // Arrange
        var product = new Product
        {
            Name = "Cancel Test Product",
            Description = "Description",
            Price = 75.00m,
            SKU = "CANCEL-001",
            StockQuantity = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var order = await _orderService.CreateOrderAsync(new CreateOrderDto
        {
            UserId = "test-user-003",
            Items = new List<CreateOrderItemDto> { new() { ProductId = product.Id, Quantity = 3 } },
            ShippingCost = 10.00m,
            Tax = 7.50m
        });

        // Verify stock was reserved
        var productAfterOrder = await _dbContext.Products.FindAsync(product.Id);
        productAfterOrder!.StockQuantity.Should().Be(17); // 20 - 3

        // Act
        var result = await _orderService.CancelOrderAsync(order.Id, new CancelOrderDto
        {
            Reason = "Customer requested cancellation"
        });

        // Assert
        result.Status.Should().Be(OrderStatus.Cancelled);
        result.CancellationReason.Should().Be("Customer requested cancellation");

        // Verify stock was released
        var productAfterCancel = await _dbContext.Products.FindAsync(product.Id);
        productAfterCancel!.StockQuantity.Should().Be(20); // Back to original
    }

    [Fact]
    public async Task CreateOrder_WithMultipleItems_ShouldHandleAtomically()
    {
        // Arrange
        var product1 = new Product { Name = "Product 1", Description = "Desc", Price = 50.00m, SKU = "MULTI-001", StockQuantity = 10, IsActive = true, CreatedAt = DateTime.UtcNow };
        var product2 = new Product { Name = "Product 2", Description = "Desc", Price = 30.00m, SKU = "MULTI-002", StockQuantity = 5, IsActive = true, CreatedAt = DateTime.UtcNow };
        _dbContext.Products.AddRange(product1, product2);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateOrderDto
        {
            UserId = "test-user-004",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = product1.Id, Quantity = 2 },
                new() { ProductId = product2.Id, Quantity = 3 }
            },
            ShippingCost = 20.00m,
            Tax = 19.00m
        };

        // Act
        var result = await _orderService.CreateOrderAsync(createDto);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalAmount.Should().Be((50.00m * 2) + (30.00m * 3)); // 100 + 90 = 190

        var p1 = await _dbContext.Products.FindAsync(product1.Id);
        var p2 = await _dbContext.Products.FindAsync(product2.Id);
        p1!.StockQuantity.Should().Be(8); // 10 - 2
        p2!.StockQuantity.Should().Be(2); // 5 - 3
    }

    [Fact]
    public async Task CreateOrder_WithOneInsufficientItem_ShouldRollbackEverything()
    {
        // Arrange
        var product1 = new Product { Name = "Product A", Description = "Desc", Price = 40.00m, SKU = "ROLLBACK-001", StockQuantity = 10, IsActive = true, CreatedAt = DateTime.UtcNow };
        var product2 = new Product { Name = "Product B", Description = "Desc", Price = 60.00m, SKU = "ROLLBACK-002", StockQuantity = 2, IsActive = true, CreatedAt = DateTime.UtcNow };
        _dbContext.Products.AddRange(product1, product2);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateOrderDto
        {
            UserId = "test-user-005",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = product1.Id, Quantity = 2 }, // OK
                new() { ProductId = product2.Id, Quantity = 5 }  // NOT OK - only 2 available
            },
            ShippingCost = 15.00m,
            Tax = 15.00m
        };

        // Act & Assert
        var act = async () => await _orderService.CreateOrderAsync(createDto);
        await act.Should().ThrowAsync<InsufficientStockException>();

        // Verify NO stock was reserved
        var p1 = await _dbContext.Products.FindAsync(product1.Id);
        var p2 = await _dbContext.Products.FindAsync(product2.Id);
        p1!.StockQuantity.Should().Be(10); // Unchanged
        p2!.StockQuantity.Should().Be(2);  // Unchanged
    }
}
