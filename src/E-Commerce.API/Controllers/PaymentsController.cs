using E_Commerce.API.DTOs;
using E_Commerce.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Get payment by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentById(Guid id)
    {
        _logger.LogInformation("GET /api/payments/{Id}", id);

        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
        {
            _logger.LogWarning("Payment not found: {Id}", id);
            return NotFound(new { message = $"Payment with ID {id} not found" });
        }

        return Ok(payment);
    }

    /// <summary>
    /// Get payment by transaction ID
    /// </summary>
    [HttpGet("transaction/{transactionId}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentByTransactionId(string transactionId)
    {
        _logger.LogInformation("GET /api/payments/transaction/{TransactionId}", transactionId);

        var payment = await _paymentService.GetPaymentByTransactionIdAsync(transactionId);
        if (payment == null)
        {
            _logger.LogWarning("Payment not found with transaction ID: {TransactionId}", transactionId);
            return NotFound(new { message = $"Payment with transaction ID {transactionId} not found" });
        }

        return Ok(payment);
    }

    /// <summary>
    /// Get all payments for an order
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetOrderPayments(Guid orderId)
    {
        _logger.LogInformation("GET /api/payments/order/{OrderId}", orderId);

        var payments = await _paymentService.GetOrderPaymentsAsync(orderId);
        return Ok(payments);
    }

    /// <summary>
    /// Process payment for an order
    /// </summary>
    [HttpPost("process")]
    public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] ProcessPaymentDto processDto)
    {
        _logger.LogInformation("POST /api/payments/process - Order: {OrderId}, Method: {PaymentMethod}",
            processDto.OrderId, processDto.PaymentMethod);

        try
        {
            var payment = await _paymentService.ProcessPaymentAsync(processDto);
            return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return BadRequest(new { message = ex.Message });
        }
    }
}
