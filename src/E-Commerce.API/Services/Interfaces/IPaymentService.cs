using E_Commerce.API.DTOs;

namespace E_Commerce.API.Services.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Get payment by ID
    /// </summary>
    Task<PaymentDto?> GetPaymentByIdAsync(Guid paymentId);

    /// <summary>
    /// Get payment by transaction ID
    /// </summary>
    Task<PaymentDto?> GetPaymentByTransactionIdAsync(string transactionId);

    /// <summary>
    /// Get all payments for an order
    /// </summary>
    Task<IEnumerable<PaymentDto>> GetOrderPaymentsAsync(Guid orderId);

    /// <summary>
    /// Process payment for an order (mock implementation)
    /// </summary>
    Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto processDto);
}
