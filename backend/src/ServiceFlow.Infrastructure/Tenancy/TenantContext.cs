using ServiceFlow.Application.Abstractions;

namespace ServiceFlow.Infrastructure.Tenancy;

/// <summary>
/// Request-scoped tenant context. Populated by authentication middleware in Phase 2.
/// Until then, HasTenant remains false and query filters exclude tenant-owned rows.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public bool HasTenant => TenantId.HasValue && TenantId.Value != Guid.Empty;

    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        TenantId = tenantId;
    }

    public void Clear()
    {
        TenantId = null;
    }
}
