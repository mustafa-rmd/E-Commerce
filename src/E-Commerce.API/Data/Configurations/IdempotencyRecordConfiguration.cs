using E_Commerce.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_Commerce.API.Data.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(i => i.RequestPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.RequestMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(i => i.RequestBody)
            .HasMaxLength(4000);

        builder.Property(i => i.ResponseStatusCode)
            .IsRequired();

        builder.Property(i => i.ResponseBody)
            .IsRequired();

        builder.Property(i => i.ResponseContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("application/json");

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired();

        // Unique constraint on IdempotencyKey + RequestPath + RequestMethod
        builder.HasIndex(i => new { i.IdempotencyKey, i.RequestPath, i.RequestMethod })
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyRecords_Key_Path_Method");

        // Index for cleanup queries (finding expired records)
        builder.HasIndex(i => i.CreatedAt)
            .HasDatabaseName("IX_IdempotencyRecords_CreatedAt");
    }
}
