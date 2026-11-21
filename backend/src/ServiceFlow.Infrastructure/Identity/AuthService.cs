using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Abstractions;
using ServiceFlow.Application.Auth;
using ServiceFlow.Domain.Enums;
using ServiceFlow.Infrastructure.Persistence;

namespace ServiceFlow.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private const string GenericLoginFailure = "Invalid email or password.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new AuthResult(false, GenericLoginFailure);
        }

        if (!user.IsActive)
        {
            return new AuthResult(false, GenericLoginFailure);
        }

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

        if (tenant is null || tenant.Status == TenantStatus.Suspended)
        {
            return new AuthResult(false, GenericLoginFailure);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            command.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return new AuthResult(false, "Account temporarily locked. Try again later.");
        }

        if (!result.Succeeded)
        {
            return new AuthResult(false, GenericLoginFailure);
        }

        return new AuthResult(true);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return null;
        }

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new CurrentUserDto(
            user.Id,
            user.TenantId,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToArray(),
            tenant.Name,
            tenant.Slug,
            tenant.Status.ToString());
    }
}
