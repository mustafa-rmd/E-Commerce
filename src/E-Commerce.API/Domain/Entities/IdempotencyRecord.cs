namespace E_Commerce.API.Domain.Entities;

/// <summary>
/// Idempotency record for preventing duplicate operations
/// </summary>
public class IdempotencyRecord : BaseEntity
{
    /// <summary>
    /// Idempotency key from X-Idempotency-Key header
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// Request path (e.g., /api/orders)
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method (POST, PATCH, etc.)
    /// </summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>
    /// Request body (for additional validation)
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// HTTP status code from original response
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// Response body from original response
    /// </summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>
    /// Content type of response
    /// </summary>
    public string ResponseContentType { get; set; } = "application/json";

    /// <summary>
    /// Check if record is expired (older than 24 hours)
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow > CreatedAt.AddHours(24);
    }
}
