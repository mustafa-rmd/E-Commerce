using E_Commerce.API.Data;
using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Repositories;

public class CartRepository : GenericRepository<Cart>, ICartRepository
{
    public CartRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Cart?> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsExpired());
    }

    public async Task<IEnumerable<Cart>> GetExpiredCartsAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(c => c.ExpiresAt <= now)
            .ToListAsync();
    }

    public async Task<Cart?> GetCartWithItemsAsync(string userId)
    {
        return await _dbSet
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<bool> HasActiveCartAsync(string userId)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .AnyAsync(c => c.UserId == userId && c.ExpiresAt > now);
    }

    public async Task<int> DeleteExpiredCartsAsync()
    {
        var expiredCarts = await GetExpiredCartsAsync();
        var count = expiredCarts.Count();

        if (count > 0)
        {
            _dbSet.RemoveRange(expiredCarts);
        }

        return count;
    }
}
