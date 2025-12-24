using E_Commerce.API.Data;
using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Repositories;

public class IdempotencyRepository : GenericRepository<IdempotencyRecord>, IIdempotencyRepository
{
    public IdempotencyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, string requestPath, string requestMethod)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.IdempotencyKey == idempotencyKey &&
                i.RequestPath == requestPath &&
                i.RequestMethod == requestMethod);
    }

    public async Task<int> DeleteExpiredRecordsAsync()
    {
        var expirationTime = DateTime.UtcNow.AddHours(-24);

        var expiredRecords = await _dbSet
            .Where(i => i.CreatedAt < expirationTime)
            .ToListAsync();

        if (expiredRecords.Any())
        {
            _dbSet.RemoveRange(expiredRecords);
            return expiredRecords.Count;
        }

        return 0;
    }
}
