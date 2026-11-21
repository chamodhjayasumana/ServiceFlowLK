using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.Entities;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(256);

        builder.Property(c => c.AddressLine1)
            .HasMaxLength(256);

        builder.Property(c => c.City)
            .HasMaxLength(100);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.Phone });

        builder.HasIndex(c => new { c.TenantId, c.FullName });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
