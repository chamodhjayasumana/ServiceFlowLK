using Microsoft.AspNetCore.Identity;

namespace ServiceFlow.Infrastructure.Identity;

/// <summary>
/// Identity user bound to a single tenant for MVP.
/// TenantId is set during registration/invitation by server-side workflows (Phase 2).
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
