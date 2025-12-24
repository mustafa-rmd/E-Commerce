using E_Commerce.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Data;

public static class SeedData
{
    public static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        // Check if products already exist
        if (context.Products.Any())
        {
            return; // Database has been seeded
        }

        var products = new List<Product>
        {
            new Product
            {
                Name = "Laptop - Dell XPS 13",
                Description = "High-performance ultrabook with 13-inch display",
                Price = 1299.99m,
                SKU = "DELL-XPS13-001",
                StockQuantity = 50,
                IsActive = true
            },
            new Product
            {
                Name = "Wireless Mouse - Logitech MX Master 3",
                Description = "Ergonomic wireless mouse for productivity",
                Price = 99.99m,
                SKU = "LOGI-MX3-001",
                StockQuantity = 150,
                IsActive = true
            },
            new Product
            {
                Name = "Mechanical Keyboard - Keychron K2",
                Description = "Wireless mechanical keyboard with RGB",
                Price = 89.99m,
                SKU = "KEY-K2-001",
                StockQuantity = 75,
                IsActive = true
            },
            new Product
            {
                Name = "USB-C Hub - Anker 7-in-1",
                Description = "Multi-port USB-C hub with HDMI and SD card reader",
                Price = 49.99m,
                SKU = "ANKR-HUB7-001",
                StockQuantity = 200,
                IsActive = true
            },
            new Product
            {
                Name = "Monitor - LG 27-inch 4K",
                Description = "27-inch 4K UHD IPS monitor",
                Price = 449.99m,
                SKU = "LG-27-4K-001",
                StockQuantity = 30,
                IsActive = true
            },
            new Product
            {
                Name = "Webcam - Logitech C920",
                Description = "1080p HD webcam for video calls",
                Price = 79.99m,
                SKU = "LOGI-C920-001",
                StockQuantity = 8,  // Low stock
                IsActive = true
            },
            new Product
            {
                Name = "Headphones - Sony WH-1000XM5",
                Description = "Noise-cancelling wireless headphones",
                Price = 399.99m,
                SKU = "SONY-XM5-001",
                StockQuantity = 45,
                IsActive = true
            },
            new Product
            {
                Name = "SSD - Samsung 1TB NVMe",
                Description = "High-speed 1TB NVMe SSD",
                Price = 129.99m,
                SKU = "SAMS-SSD1TB-001",
                StockQuantity = 5,  // Low stock
                IsActive = true
            },
            new Product
            {
                Name = "Phone Stand - Adjustable Aluminum",
                Description = "Adjustable phone/tablet stand",
                Price = 24.99m,
                SKU = "STAND-ALU-001",
                StockQuantity = 120,
                IsActive = true
            },
            new Product
            {
                Name = "Power Bank - Anker 20000mAh",
                Description = "High-capacity portable power bank",
                Price = 59.99m,
                SKU = "ANKR-PB20K-001",
                StockQuantity = 90,
                IsActive = true
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }

    public static async Task SeedCartsAsync(ApplicationDbContext context)
    {
        // Check if carts already exist
        if (context.Carts.Any())
        {
            return; // Database has been seeded
        }

        // Get some products for cart items
        var products = await context.Products.Take(5).ToListAsync();
        if (!products.Any())
        {
            return; // No products available
        }

        var carts = new List<Cart>
        {
            new Cart
            {
                UserId = "user1@example.com",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        ProductId = products[0].Id,
                        Quantity = 1,
                        UnitPrice = products[0].Price
                    },
                    new CartItem
                    {
                        ProductId = products[1].Id,
                        Quantity = 2,
                        UnitPrice = products[1].Price
                    }
                }
            },
            new Cart
            {
                UserId = "user2@example.com",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        ProductId = products[2].Id,
                        Quantity = 1,
                        UnitPrice = products[2].Price
                    },
                    new CartItem
                    {
                        ProductId = products[3].Id,
                        Quantity = 3,
                        UnitPrice = products[3].Price
                    },
                    new CartItem
                    {
                        ProductId = products[4].Id,
                        Quantity = 1,
                        UnitPrice = products[4].Price
                    }
                }
            }
        };

        await context.Carts.AddRangeAsync(carts);
        await context.SaveChangesAsync();
    }
}
