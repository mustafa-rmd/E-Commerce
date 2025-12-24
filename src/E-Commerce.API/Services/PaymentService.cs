using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Domain.Enums;
using E_Commerce.API.Domain.Exceptions;
using E_Commerce.API.DTOs;
using E_Commerce.API.Repositories.Interfaces;
using E_Commerce.API.Services.Interfaces;

namespace E_Commerce.API.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IUnitOfWork unitOfWork, ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        _logger.LogInformation("Getting payment: {PaymentId}", paymentId);

        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        return payment == null ? null : MapToDto(payment);
    }

    public async Task<PaymentDto?> GetPaymentByTransactionIdAsync(string transactionId)
    {
        _logger.LogInformation("Getting payment by transaction ID: {TransactionId}", transactionId);

        var payment = await _unitOfWork.Payments.GetByTransactionIdAsync(transactionId);
        return payment == null ? null : MapToDto(payment);
    }

    public async Task<IEnumerable<PaymentDto>> GetOrderPaymentsAsync(Guid orderId)
    {
        _logger.LogInformation("Getting payments for order: {OrderId}", orderId);

        var payments = await _unitOfWork.Payments.GetPaymentsByOrderIdAsync(orderId);
        return payments.Select(MapToDto);
    }

    public async Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto processDto)
    {
        _logger.LogInformation("Processing payment for order: {OrderId} with method: {PaymentMethod}",
            processDto.OrderId, processDto.PaymentMethod);

        using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Get the order
            var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(processDto.OrderId);
            if (order == null)
            {
                throw new DomainException($"Order with ID {processDto.OrderId} not found");
            }

            // Validate order is in Confirmed status
            if (order.Status != OrderStatus.Confirmed)
            {
                throw new DomainException($"Order must be in Confirmed status to process payment. Current status: {order.Status}");
            }

            // Check if payment already exists for this order
            var existingPayment = await _unitOfWork.Payments.GetOrderPaymentByStatusAsync(
                processDto.OrderId, PaymentStatus.Completed);
            if (existingPayment != null)
            {
                throw new DomainException($"Payment already processed for order {order.OrderNumber}");
            }

            // Create payment record
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.GrandTotal,
                PaymentMethod = processDto.PaymentMethod,
                Status = PaymentStatus.Pending
            };

            await _unitOfWork.Payments.AddAsync(payment);

            // MOCK: Simulate payment processing
            // In a real system, this would call an external payment gateway
            var success = await SimulatePaymentGatewayAsync(payment.Amount, processDto.PaymentMethod);

            if (success)
            {
                // Payment successful
                var transactionId = Payment.GenerateTransactionId();
                payment.ProcessPayment(transactionId);

                // Update order status to Paid
                order.MarkAsPaid();
                await _unitOfWork.Orders.UpdateAsync(order);

                _logger.LogInformation("Payment processed successfully. Transaction ID: {TransactionId}", transactionId);
            }
            else
            {
                // Payment failed
                payment.Fail("Insufficient funds (mock)");
                _logger.LogWarning("Payment failed for order: {OrderId}", processDto.OrderId);
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToDto(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order: {OrderId}", processDto.OrderId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Mock payment gateway simulation
    /// In a real system, this would call Stripe, PayPal, etc.
    /// </summary>
    private async Task<bool> SimulatePaymentGatewayAsync(decimal amount, string paymentMethod)
    {
        // Simulate network delay
        await Task.Delay(500);

        // Mock logic:
        // - Fail if amount > $10,000 (simulating card limit)
        // - Fail if payment method is "FailCard" (for testing)
        // - Otherwise succeed
        if (amount > 10000m)
        {
            _logger.LogWarning("Mock payment gateway: Amount ${Amount} exceeds limit", amount);
            return false;
        }

        if (paymentMethod.Equals("FailCard", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Mock payment gateway: FailCard method triggered");
            return false;
        }

        _logger.LogInformation("Mock payment gateway: Payment of ${Amount} approved", amount);
        return true;
    }

    private PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Status = payment.Status,
            PaymentMethod = payment.PaymentMethod,
            TransactionId = payment.TransactionId,
            ProcessedAt = payment.ProcessedAt,
            FailedAt = payment.FailedAt,
            FailureReason = payment.FailureReason,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}
