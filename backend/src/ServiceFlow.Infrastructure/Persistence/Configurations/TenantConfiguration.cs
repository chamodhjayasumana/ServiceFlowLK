using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.Entities;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.Property(t => t.ContactEmail)
            .HasMaxLength(256);

        builder.Property(t => t.ContactPhone)
            .HasMaxLength(32);

        builder.Property(t => t.AddressLine1)
            .HasMaxLength(256);

        builder.Property(t => t.AddressLine2)
            .HasMaxLength(256);

        builder.Property(t => t.City)
            .HasMaxLength(100);

        builder.Property(t => t.TimeZoneId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();
    }
}
