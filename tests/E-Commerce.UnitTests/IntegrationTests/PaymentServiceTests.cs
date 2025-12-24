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
public class PaymentServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private ApplicationDbContext _dbContext = null!;
    private PaymentService _paymentService = null!;
    private OrderService _orderService = null!;

    public PaymentServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _dbContext = await _fixture.CreateCleanDbContextAsync();
        var unitOfWork = new UnitOfWork(_dbContext);
        var paymentLogger = new Mock<ILogger<PaymentService>>().Object;
        var orderLogger = new Mock<ILogger<OrderService>>().Object;

        _paymentService = new PaymentService(unitOfWork, paymentLogger);
        _orderService = new OrderService(unitOfWork, _dbContext, orderLogger);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task ProcessPayment_ShouldSucceed_WithValidPaymentMethod()
    {
        // Arrange
        var product = new Product
        {
            Name = "Payment Test Product",
            Description = "Description",
            Price = 150.00m,
            SKU = "PAY-001",
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var order = await _orderService.CreateOrderAsync(new CreateOrderDto
        {
            UserId = "payment-user-001",
            Items = new List<CreateOrderItemDto> { new() { ProductId = product.Id, Quantity = 1 } },
            ShippingCost = 10.00m,
            Tax = 16.00m
        });

        await _orderService.ConfirmOrderAsync(order.Id);

        var processPaymentDto = new ProcessPaymentDto
        {
            OrderId = order.Id,
            PaymentMethod = "CreditCard"
        };

        // Act
        var result = await _paymentService.ProcessPaymentAsync(processPaymentDto);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Completed);
        result.TransactionId.Should().NotBeNullOrEmpty();
        result.TransactionId.Should().StartWith("TXN-");
        result.ProcessedAt.Should().NotBeNull();
        result.Amount.Should().Be(order.GrandTotal);

        // Verify order was marked as paid
        var updatedOrder = await _orderService.GetOrderByIdAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Paid);
        updatedOrder.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPayment_ShouldFail_WithFailCard()
    {
        // Arrange
        var product = new Product
        {
            Name = "Fail Payment Product",
            Description = "Description",
            Price = 50.00m,
            SKU = "FAIL-PAY-001",
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var order = await _orderService.CreateOrderAsync(new CreateOrderDto
        {
            UserId = "payment-user-002",
            Items = new List<CreateOrderItemDto> { new() { ProductId = product.Id, Quantity = 1 } },
            ShippingCost = 5.00m,
            Tax = 5.50m
        });

        await _orderService.ConfirmOrderAsync(order.Id);

        var processPaymentDto = new ProcessPaymentDto
        {
            OrderId = order.Id,
            PaymentMethod = "FailCard"
        };

        // Act
        var result = await _paymentService.ProcessPaymentAsync(processPaymentDto);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Failed);
        result.FailedAt.Should().NotBeNull();
        result.FailureReason.Should().Contain("Insufficient funds");
        result.TransactionId.Should().BeNull();

        // Verify order is still in Confirmed status
        var updatedOrder = await _orderService.GetOrderByIdAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Confirmed);
        updatedOrder.PaidAt.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPayment_ShouldFail_WhenAmountExceedsLimit()
    {
        // Arrange
        var expensiveProduct = new Product
        {
            Name = "Expensive Product",
            Description = "Very expensive",
            Price = 11000.00m, // Over $10k limit
            SKU = "EXPENSIVE-001",
            StockQuantity = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(expensiveProduct);
        await _dbContext.SaveChangesAsync();

        var order = await _orderService.CreateOrderAsync(new CreateOrderDto
        {
            UserId = "payment-user-003",
            Items = new List<CreateOrderItemDto> { new() { ProductId = expensiveProduct.Id, Quantity = 1 } },
            ShippingCost = 0m,
            Tax = 0m
        });

        await _orderService.ConfirmOrderAsync(order.Id);

        var processPaymentDto = new ProcessPaymentDto
        {
            OrderId = order.Id,
            PaymentMethod = "CreditCard"
        };

        // Act
        var result = await _paymentService.ProcessPaymentAsync(processPaymentDto);

        // Assert
        result.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task ProcessPayment_ShouldThrowException_WhenOrderNotConfirmed()
    {
        // Arrange
        var product = new Product
        {
            Name = "Not Confirmed Product",
            Description = "Description",
            Price = 75.00m,
            SKU = "NOT-CONF-001",
            StockQuantity = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var order = await _orderService.CreateOrderAsync(new CreateOrderDto
        {
            UserId = "payment-user-004",
            Items = new List<CreateOrderItemDto> { new() { ProductId = product.Id, Quantity = 1 } },
            ShippingCost = 10.00m,
            Tax = 7.50m
        });

        // Don't confirm the order

        var processPaymentDto = new ProcessPaymentDto
        {
            OrderId = order.Id,
            PaymentMethod = "CreditCard"
        };

        // Act & Assert
        var act = async () => await _paymentService.ProcessPaymentAsync(processPaymentDto);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*must be in Confirmed status*");
    }

    [Fact]
    public async Task ProcessPayment_ShouldThrowException_WhenAlreadyPaid()
    {
        // Arrange
        var product = new Product
        {
            Name = "Already Paid Product",
            Description = "Description",
            Price = 100.00m,
            SKU = "ALREADY-PAID-001",
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var order = await _orderService.CreateOrderAsync(new CreateOrderDto
        {
            UserId = "payment-user-005",
            Items = new List<CreateOrderItemDto> { new() { ProductId = product.Id, Quantity = 1 } },
            ShippingCost = 10.00m,
            Tax = 11.00m
        });

        await _orderService.ConfirmOrderAsync(order.Id);

        // Process payment first time
        await _paymentService.ProcessPaymentAsync(new ProcessPaymentDto
        {
            OrderId = order.Id,
            PaymentMethod = "CreditCard"
        });

        // Try to process payment again
        var processPaymentDto = new ProcessPaymentDto
        {
            OrderId = order.Id,
            PaymentMethod = "CreditCard"
        };

        // Act & Assert
        var act = async () => await _paymentService.ProcessPaymentAsync(processPaymentDto);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Paid*"); // Order is in Paid status, not Confirmed
    }
}
