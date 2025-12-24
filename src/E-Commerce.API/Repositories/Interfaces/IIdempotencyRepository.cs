using E_Commerce.API.Domain.Entities;

namespace E_Commerce.API.Repositories.Interfaces;

public interface IIdempotencyRepository : IGenericRepository<IdempotencyRecord>
{
    /// <summary>
    /// Get idempotency record by key, path, and method
    /// </summary>
    Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, string requestPath, string requestMethod);

    /// <summary>
    /// Delete expired idempotency records (older than 24 hours)
    /// </summary>
    Task<int> DeleteExpiredRecordsAsync();
}
