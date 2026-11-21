using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.Enums;

namespace ServiceFlow.Domain.Entities;

public class EmployeeInvitation : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string RoleName { get; private set; } = string.Empty;

    /// <summary>SHA-256 hash of the invitation token. Raw token is never stored.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public Guid InvitedByUserId { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? AcceptedAtUtc { get; private set; }

    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;

    private EmployeeInvitation()
    {
    }

    public static EmployeeInvitation Create(
        Guid tenantId,
        string email,
        string roleName,
        string tokenHash,
        Guid invitedByUserId,
        DateTimeOffset expiresAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (invitedByUserId == Guid.Empty)
        {
            throw new ArgumentException("InvitedByUserId is required.", nameof(invitedByUserId));
        }

        if (expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAtUtc));
        }

        return new EmployeeInvitation
        {
            TenantId = tenantId,
            Email = email.Trim().ToLowerInvariant(),
            RoleName = roleName.Trim(),
            TokenHash = tokenHash,
            InvitedByUserId = invitedByUserId,
            ExpiresAtUtc = expiresAtUtc,
            Status = InvitationStatus.Pending
        };
    }

    public bool IsUsable(DateTimeOffset utcNow) =>
        Status == InvitationStatus.Pending && ExpiresAtUtc > utcNow;

    public void MarkAccepted(DateTimeOffset utcNow)
    {
        if (!IsUsable(utcNow))
        {
            throw new InvalidOperationException("Invitation is not usable.");
        }

        Status = InvitationStatus.Accepted;
        AcceptedAtUtc = utcNow;
        Touch();
    }

    public void Revoke()
    {
        if (Status == InvitationStatus.Accepted)
        {
            throw new InvalidOperationException("Accepted invitations cannot be revoked.");
        }

        Status = InvitationStatus.Revoked;
        Touch();
    }

    public void MarkExpired()
    {
        if (Status == InvitationStatus.Pending)
        {
            Status = InvitationStatus.Expired;
            Touch();
        }
    }
}
