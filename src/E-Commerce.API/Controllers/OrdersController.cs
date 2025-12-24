using E_Commerce.API.Domain.Enums;
using E_Commerce.API.DTOs;
using E_Commerce.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id)
    {
        _logger.LogInformation("GET /api/orders/{Id}", id);

        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Order not found: {Id}", id);
            return NotFound(new { message = $"Order with ID {id} not found" });
        }

        return Ok(order);
    }

    /// <summary>
    /// Get order by order number
    /// </summary>
    [HttpGet("number/{orderNumber}")]
    public async Task<ActionResult<OrderDto>> GetOrderByNumber(string orderNumber)
    {
        _logger.LogInformation("GET /api/orders/number/{OrderNumber}", orderNumber);

        var order = await _orderService.GetOrderByNumberAsync(orderNumber);
        if (order == null)
        {
            _logger.LogWarning("Order not found: {OrderNumber}", orderNumber);
            return NotFound(new { message = $"Order with number {orderNumber} not found" });
        }

        return Ok(order);
    }

    /// <summary>
    /// Get all orders for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetUserOrders(string userId, [FromQuery] OrderStatus? status = null)
    {
        _logger.LogInformation("GET /api/orders/user/{UserId}?status={Status}", userId, status);

        var orders = status.HasValue
            ? await _orderService.GetUserOrdersByStatusAsync(userId, status.Value)
            : await _orderService.GetUserOrdersAsync(userId);

        return Ok(orders);
    }

    /// <summary>
    /// Create order from user's cart
    /// </summary>
    [HttpPost("from-cart")]
    public async Task<ActionResult<OrderDto>> CreateOrderFromCart([FromBody] CreateOrderFromCartDto createDto)
    {
        _logger.LogInformation("POST /api/orders/from-cart - User: {UserId}", createDto.UserId);

        try
        {
            var order = await _orderService.CreateOrderFromCartAsync(createDto);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order from cart");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create order directly
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createDto)
    {
        _logger.LogInformation("POST /api/orders - User: {UserId}, Items: {ItemCount}",
            createDto.UserId, createDto.Items.Count);

        try
        {
            var order = await _orderService.CreateOrderAsync(createDto);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Confirm order
    /// </summary>
    [HttpPatch("{id:guid}/confirm")]
    public async Task<ActionResult<OrderDto>> ConfirmOrder(Guid id)
    {
        _logger.LogInformation("PATCH /api/orders/{Id}/confirm", id);

        try
        {
            var order = await _orderService.ConfirmOrderAsync(id);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Mark order as paid
    /// </summary>
    [HttpPatch("{id:guid}/pay")]
    public async Task<ActionResult<OrderDto>> MarkOrderAsPaid(Guid id)
    {
        _logger.LogInformation("PATCH /api/orders/{Id}/pay", id);

        try
        {
            var order = await _orderService.MarkOrderAsPaidAsync(id);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking order as paid");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Ship order
    /// </summary>
    [HttpPatch("{id:guid}/ship")]
    public async Task<ActionResult<OrderDto>> ShipOrder(Guid id)
    {
        _logger.LogInformation("PATCH /api/orders/{Id}/ship", id);

        try
        {
            var order = await _orderService.ShipOrderAsync(id);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping order");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deliver order
    /// </summary>
    [HttpPatch("{id:guid}/deliver")]
    public async Task<ActionResult<OrderDto>> DeliverOrder(Guid id)
    {
        _logger.LogInformation("PATCH /api/orders/{Id}/deliver", id);

        try
        {
            var order = await _orderService.DeliverOrderAsync(id);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering order");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel order
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> CancelOrder(Guid id, [FromBody] CancelOrderDto cancelDto)
    {
        _logger.LogInformation("PATCH /api/orders/{Id}/cancel - Reason: {Reason}", id, cancelDto.Reason);

        try
        {
            var order = await _orderService.CancelOrderAsync(id, cancelDto);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order");
            return BadRequest(new { message = ex.Message });
        }
    }
}
