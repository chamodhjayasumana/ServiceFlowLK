namespace ServiceFlow.Domain.Common;

/// <summary>
/// Marker for entities owned by a single tenant.
/// TenantId must be assigned by trusted server-side code, never from client input as authorization authority.
/// </summary>
public interface ITenantOwned
{
    Guid TenantId { get; }
}
