using E_Commerce.API.Data;
using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Domain.Enums;
using E_Commerce.API.Domain.Exceptions;
using E_Commerce.API.DTOs;
using E_Commerce.API.Repositories.Interfaces;
using E_Commerce.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId)
    {
        _logger.LogInformation("Getting order: {OrderId}", orderId);

        var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber)
    {
        _logger.LogInformation("Getting order by number: {OrderNumber}", orderNumber);

        var order = await _unitOfWork.Orders.GetByOrderNumberAsync(orderNumber);
        return order == null ? null : MapToDto(order);
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
    {
        _logger.LogInformation("Getting orders for user: {UserId}", userId);

        var orders = await _unitOfWork.Orders.GetOrdersByUserIdAsync(userId);
        return orders.Select(MapToDto);
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersByStatusAsync(string userId, OrderStatus status)
    {
        _logger.LogInformation("Getting {Status} orders for user: {UserId}", status, userId);

        var orders = await _unitOfWork.Orders.GetUserOrdersByStatusAsync(userId, status);
        return orders.Select(MapToDto);
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(CreateOrderFromCartDto createDto)
    {
        _logger.LogInformation("Creating order from cart for user: {UserId}", createDto.UserId);

        // Get user's cart
        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(createDto.UserId);
        if (cart == null)
        {
            throw new DomainException($"Cart not found for user {createDto.UserId}");
        }

        if (!cart.Items.Any())
        {
            throw new DomainException("Cannot create order from empty cart");
        }

        // Convert cart items to order items
        var orderItems = new List<CreateOrderItemDto>();
        foreach (var cartItem in cart.Items)
        {
            orderItems.Add(new CreateOrderItemDto
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity
            });
        }

        // Create order
        var order = await CreateOrderAsync(new CreateOrderDto
        {
            UserId = createDto.UserId,
            Items = orderItems,
            ShippingCost = createDto.ShippingCost,
            Tax = createDto.Tax
        });

        // Clear cart after successful order creation
        cart.Clear();
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} created from cart for user: {UserId}", order.OrderNumber, createDto.UserId);

        return order;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createDto)
    {
        _logger.LogInformation("Creating order for user: {UserId} with {ItemCount} items",
            createDto.UserId, createDto.Items.Count);

        using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Step 1: Validate ALL items have sufficient stock FIRST
            var productsToReserve = new List<(Product Product, int Quantity)>();
            decimal totalAmount = 0m;

            foreach (var item in createDto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    throw new DomainException($"Product with ID {item.ProductId} not found");
                }

                if (!product.IsActive)
                {
                    throw new DomainException($"Product {product.SKU} is not active");
                }

                // Validate stock (will throw if insufficient)
                if (!product.HasSufficientStock(item.Quantity))
                {
                    throw new InsufficientStockException(
                        $"Product {product.SKU} has insufficient stock. Requested: {item.Quantity}, Available: {product.StockQuantity}");
                }

                productsToReserve.Add((product, item.Quantity));
                totalAmount += product.Price * item.Quantity;
            }

            // Step 2: Create order with price snapshots
            var order = new Order
            {
                OrderNumber = Order.GenerateOrderNumber(),
                UserId = createDto.UserId,
                Status = OrderStatus.Pending,
                TotalAmount = totalAmount,
                ShippingCost = createDto.ShippingCost,
                Tax = createDto.Tax
            };

            // Create order items with price snapshots
            foreach (var (product, quantity) in productsToReserve)
            {
                var orderItem = OrderItem.FromProduct(product, quantity);
                orderItem.Product = product; // Set navigation property
                order.Items.Add(orderItem);

                // Mark as Added for EF Core
                _dbContext.Entry(orderItem).State = EntityState.Added;
            }

            await _unitOfWork.Orders.AddAsync(order);

            // Step 3: Reserve stock (atomic with order creation)
            foreach (var (product, quantity) in productsToReserve)
            {
                product.ReserveStock(quantity);
                await _unitOfWork.Products.UpdateAsync(product);
            }

            // Step 4: Commit transaction (all or nothing)
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderNumber} created successfully. Total: {TotalAmount}, Items reserved",
                order.OrderNumber, order.TotalAmount);

            return MapToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user: {UserId}", createDto.UserId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<OrderDto> ConfirmOrderAsync(Guid orderId)
    {
        _logger.LogInformation("Confirming order: {OrderId}", orderId);

        var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new DomainException($"Order with ID {orderId} not found");
        }

        order.Confirm();
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} confirmed", order.OrderNumber);

        return MapToDto(order);
    }

    public async Task<OrderDto> MarkOrderAsPaidAsync(Guid orderId)
    {
        _logger.LogInformation("Marking order as paid: {OrderId}", orderId);

        var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new DomainException($"Order with ID {orderId} not found");
        }

        order.MarkAsPaid();
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} marked as paid", order.OrderNumber);

        return MapToDto(order);
    }

    public async Task<OrderDto> ShipOrderAsync(Guid orderId)
    {
        _logger.LogInformation("Shipping order: {OrderId}", orderId);

        var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new DomainException($"Order with ID {orderId} not found");
        }

        order.Ship();
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} shipped", order.OrderNumber);

        return MapToDto(order);
    }

    public async Task<OrderDto> DeliverOrderAsync(Guid orderId)
    {
        _logger.LogInformation("Delivering order: {OrderId}", orderId);

        var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new DomainException($"Order with ID {orderId} not found");
        }

        order.Deliver();
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} delivered", order.OrderNumber);

        return MapToDto(order);
    }

    public async Task<OrderDto> CancelOrderAsync(Guid orderId, CancelOrderDto cancelDto)
    {
        _logger.LogInformation("Cancelling order: {OrderId}", orderId);

        using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
            if (order == null)
            {
                throw new DomainException($"Order with ID {orderId} not found");
            }

            // Cancel order (will throw if not allowed)
            order.Cancel(cancelDto.Reason);

            // Release reserved stock back to products
            foreach (var item in order.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.ReleaseStock(item.Quantity);
                    await _unitOfWork.Products.UpdateAsync(product);
                    _logger.LogInformation("Released {Quantity} units of {SKU} back to stock",
                        item.Quantity, product.SKU);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderNumber} cancelled. Reason: {Reason}",
                order.OrderNumber, cancelDto.Reason);

            return MapToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order: {OrderId}", orderId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    private OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            ShippingCost = order.ShippingCost,
            Tax = order.Tax,
            GrandTotal = order.GrandTotal,
            ConfirmedAt = order.ConfirmedAt,
            PaidAt = order.PaidAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            CancellationReason = order.CancellationReason,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(item => new OrderItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductSKU = item.ProductSKU,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal
            }).ToList()
        };
    }
}
