using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace E_Commerce.API.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use the connection string for design-time migrations
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=ECommerceDb;Username=ecommerce_user;Password=ecommerce_pass_2025");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
