using Microsoft.EntityFrameworkCore.Storage;

namespace E_Commerce.API.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ICartRepository Carts { get; }
    IOrderRepository Orders { get; }
    IPaymentRepository Payments { get; }
    IIdempotencyRepository IdempotencyRecords { get; }

    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}
