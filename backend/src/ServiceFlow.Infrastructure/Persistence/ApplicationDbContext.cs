using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Abstractions;
using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.Entities;
using ServiceFlow.Infrastructure.Identity;

namespace ServiceFlow.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<EmployeeInvitation> EmployeeInvitations => Set<EmployeeInvitation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
            entity.HasIndex(u => u.TenantId);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Defense-in-depth: tenant-owned entities are filtered by trusted server tenant context.
        // When no tenant is set, TenantId resolves to Guid.Empty and no owned rows match.
        builder.Entity<Customer>()
            .HasQueryFilter(c => c.TenantId == CurrentTenantId);

        builder.Entity<EmployeeInvitation>()
            .HasQueryFilter(i => i.TenantId == CurrentTenantId);
    }

    private Guid CurrentTenantId => _tenantContext.TenantId ?? Guid.Empty;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceTenantOwnershipOnSave();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void EnforceTenantOwnershipOnSave()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (!_tenantContext.HasTenant)
            {
                // Platform/bootstrap operations may run without a tenant context.
                // Still reject empty TenantId values on owned entities.
                if (entry.Entity.TenantId == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        "Tenant-owned entities require a non-empty TenantId assigned by the server.");
                }

                continue;
            }

            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                // Prefer explicit assignment in application code; this is a safety net only when context exists.
                entry.Property(nameof(ITenantOwned.TenantId)).CurrentValue = _tenantContext.TenantId!.Value;
            }

            if (entry.Entity.TenantId != _tenantContext.TenantId)
            {
                throw new InvalidOperationException(
                    "Cross-tenant write rejected. TenantId must match the authenticated tenant context.");
            }
        }
    }
}
