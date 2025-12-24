using E_Commerce.API.Domain.Enums;
using E_Commerce.API.Domain.Exceptions;

namespace E_Commerce.API.Domain.Entities;

/// <summary>
/// Payment entity for tracking order payments
/// </summary>
public class Payment : BaseEntity
{
    /// <summary>
    /// Foreign key to Order
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Navigation property to Order
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment status
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Payment method (e.g., CreditCard, PayPal, BankTransfer)
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Transaction ID from payment gateway (mock for now)
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Timestamp when payment was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Timestamp when payment failed
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Failure reason if payment failed
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Process payment (mock implementation)
    /// </summary>
    public void ProcessPayment(string transactionId)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Cannot process payment in {Status} status");

        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark payment as failed
    /// </summary>
    public void Fail(string reason)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Cannot fail payment in {Status} status");

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        FailedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generate mock transaction ID
    /// </summary>
    public static string GenerateTransactionId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(100000, 999999);
        return $"TXN-{timestamp}-{random}";
    }
}
