namespace ServiceFlow.Application.Abstractions;

/// <summary>
/// Provides the active tenant for the current request.
/// Must be resolved from authenticated user membership, never from arbitrary client headers.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }

    bool HasTenant { get; }
}
