using E_Commerce.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace E_Commerce.UnitTests.Infrastructure;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ECommerceTestDb")
        .WithUsername("test_user")
        .WithPassword("test_password_2025")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();

        // Apply migrations
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task<ApplicationDbContext> CreateCleanDbContextAsync()
    {
        await using var context = CreateDbContext();

        // Clean all tables
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"IdempotencyRecords\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Payments\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"OrderItems\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Orders\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"CartItems\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Carts\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Products\" CASCADE");

        return CreateDbContext();
    }
}
