using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Domain.Enums;

namespace E_Commerce.API.Repositories.Interfaces;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    /// <summary>
    /// Get payment by transaction ID
    /// </summary>
    Task<Payment?> GetByTransactionIdAsync(string transactionId);

    /// <summary>
    /// Get all payments for a specific order
    /// </summary>
    Task<IEnumerable<Payment>> GetPaymentsByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Get payments by status
    /// </summary>
    Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);

    /// <summary>
    /// Get payment for order with specific status
    /// </summary>
    Task<Payment?> GetOrderPaymentByStatusAsync(Guid orderId, PaymentStatus status);
}
