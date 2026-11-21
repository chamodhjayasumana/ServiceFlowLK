using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Abstractions;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Tenants;
using ServiceFlow.Domain.Constants;
using ServiceFlow.Domain.Entities;
using ServiceFlow.Infrastructure.Persistence;

namespace ServiceFlow.Infrastructure.Identity;

public sealed class TenantRegistrationService : ITenantRegistrationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public TenantRegistrationService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<RegisterTenantResult> RegisterAsync(
        RegisterTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var slug = command.Slug.Trim().ToLowerInvariant();
        var ownerEmail = command.OwnerEmail.Trim().ToLowerInvariant();

        if (await _dbContext.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken))
        {
            throw new ConflictException("A business with this slug already exists.");
        }

        if (await _userManager.FindByEmailAsync(ownerEmail) is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        if (!await _roleManager.RoleExistsAsync(AppRoles.Owner))
        {
            throw new InvalidOperationException("Application roles are not seeded.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var tenant = Tenant.Create(
                    command.BusinessName,
                    slug,
                    command.ContactEmail ?? ownerEmail,
                    command.ContactPhone,
                    command.AddressLine1,
                    city: command.City);

                tenant.Activate();
                _dbContext.Tenants.Add(tenant);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var owner = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = ownerEmail,
                    Email = ownerEmail,
                    EmailConfirmed = true,
                    DisplayName = command.OwnerDisplayName.Trim(),
                    TenantId = tenant.Id,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                var createResult = await _userManager.CreateAsync(owner, command.OwnerPassword);
                if (!createResult.Succeeded)
                {
                    var detail = string.Join(" ", createResult.Errors.Select(e => e.Description));
                    throw new AppException(string.IsNullOrWhiteSpace(detail)
                        ? "Unable to create the owner account."
                        : detail);
                }

                var roleResult = await _userManager.AddToRoleAsync(owner, AppRoles.Owner);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Failed to assign the Owner role.");
                }

                await transaction.CommitAsync(cancellationToken);

                return new RegisterTenantResult(tenant.Id, tenant.Slug, owner.Id, ownerEmail);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
