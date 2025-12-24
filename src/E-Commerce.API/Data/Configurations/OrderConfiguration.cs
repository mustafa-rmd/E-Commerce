using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_Commerce.API.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.ShippingCost)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Tax)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.CancellationReason)
            .HasMaxLength(500);

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(o => o.UpdatedAt)
            .IsRequired(false);

        // Unique index on OrderNumber
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        // Index for user order lookups
        builder.HasIndex(o => o.UserId);

        // Index for status queries
        builder.HasIndex(o => o.Status);

        // Composite index for user + status queries
        builder.HasIndex(o => new { o.UserId, o.Status });

        // One-to-many relationship with OrderItems
        builder.HasMany(o => o.Items)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
