using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Abstractions;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Users;
using ServiceFlow.Domain.Constants;
using ServiceFlow.Domain.Entities;
using ServiceFlow.Domain.Enums;
using ServiceFlow.Infrastructure.Persistence;

namespace ServiceFlow.Infrastructure.Identity;

public sealed class EmployeeInvitationService : IEmployeeInvitationService
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public EmployeeInvitationService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ITenantContext tenantContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<InviteEmployeeResult> InviteAsync(
        InviteEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticatedTenant();

        if (!_currentUser.Roles.Contains(AppRoles.Owner) &&
            !_currentUser.Roles.Contains(AppRoles.Manager))
        {
            throw new ForbiddenAppException();
        }

        if (_currentUser.Roles.Contains(AppRoles.Manager) &&
            !string.Equals(command.RoleName, AppRoles.Receptionist, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(command.RoleName, AppRoles.Technician, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAppException("Managers can only invite Receptionist or Technician roles.");
        }

        if (string.Equals(command.RoleName, AppRoles.Owner, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAppException("Owner role cannot be assigned by invitation.");
        }

        var email = command.Email.Trim().ToLowerInvariant();
        var tenantId = _tenantContext.TenantId!.Value;

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        // Filter expiry/status in-memory: SQLite test provider cannot translate DateTimeOffset comparisons reliably.
        var utcNow = DateTimeOffset.UtcNow;
        var existingForEmail = await _dbContext.EmployeeInvitations
            .Where(i => i.Email == email)
            .ToListAsync(cancellationToken);
        var hasPending = existingForEmail.Any(i =>
            i.Status == InvitationStatus.Pending && i.ExpiresAtUtc > utcNow);

        if (hasPending)
        {
            throw new ConflictException("A pending invitation already exists for this email.");
        }

        var rawToken = InvitationTokenHasher.CreateToken();
        var invitation = EmployeeInvitation.Create(
            tenantId,
            email,
            command.RoleName,
            InvitationTokenHasher.Hash(rawToken),
            _currentUser.UserId!.Value,
            DateTimeOffset.UtcNow.Add(InvitationLifetime));

        _dbContext.EmployeeInvitations.Add(invitation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Raw token returned once to the inviter for secure out-of-band delivery. Never emailed as a password.
        return new InviteEmployeeResult(
            invitation.Id,
            invitation.Email,
            invitation.RoleName,
            invitation.ExpiresAtUtc,
            rawToken);
    }

    public async Task<AcceptInvitationResult> AcceptAsync(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = InvitationTokenHasher.Hash(command.Token.Trim());
        var invitation = await _dbContext.EmployeeInvitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

        if (invitation is null || !invitation.IsUsable(DateTimeOffset.UtcNow))
        {
            if (invitation is not null && invitation.Status == InvitationStatus.Pending)
            {
                invitation.MarkExpired();
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            throw new AppException("Invitation is invalid or expired.");
        }

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == invitation.TenantId, cancellationToken);

        if (tenant is null || tenant.Status == TenantStatus.Suspended)
        {
            throw new AppException("The business account is unavailable.");
        }

        if (await _userManager.FindByEmailAsync(invitation.Email) is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        if (!AppRoles.IsAssignableByInvitation(invitation.RoleName))
        {
            throw new ForbiddenAppException("Invitation role is not permitted.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = invitation.Email,
                    Email = invitation.Email,
                    EmailConfirmed = true,
                    DisplayName = command.DisplayName.Trim(),
                    TenantId = invitation.TenantId,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, command.Password);
                if (!createResult.Succeeded)
                {
                    var detail = string.Join(" ", createResult.Errors.Select(e => e.Description));
                    throw new AppException(string.IsNullOrWhiteSpace(detail)
                        ? "Unable to create the employee account."
                        : detail);
                }

                var roleResult = await _userManager.AddToRoleAsync(user, invitation.RoleName);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Failed to assign the invited role.");
                }

                invitation.MarkAccepted(DateTimeOffset.UtcNow);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new AcceptInvitationResult(user.Id, user.TenantId, user.Email!, invitation.RoleName);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<IReadOnlyList<EmployeeListItemDto>> ListEmployeesAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticatedTenant();

        var tenantId = _tenantContext.TenantId!.Value;
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);

        var result = new List<EmployeeListItemDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new EmployeeListItemDto(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.IsActive,
                roles.ToArray()));
        }

        return result;
    }

    private void EnsureAuthenticatedTenant()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null || !_tenantContext.HasTenant)
        {
            throw new UnauthorizedAppException();
        }
    }
}
