using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Domain.Enums;
using ServiceFlow.Infrastructure.Identity;
using ServiceFlow.Infrastructure.Persistence;

namespace ServiceFlow.Infrastructure.Tenancy;

/// <summary>
/// Resolves tenant context exclusively from the authenticated user's stored membership.
/// Client-supplied TenantId headers/body values are ignored.
/// </summary>
public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdValue = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { title = "Unauthorized", detail = "Invalid session principal." });
                return;
            }

            var user = await userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null || !user.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { title = "Unauthorized", detail = "User account is inactive or missing." });
                return;
            }

            var tenant = await dbContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == user.TenantId);

            if (tenant is null || tenant.Status == TenantStatus.Suspended)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { title = "Forbidden", detail = "Tenant is unavailable." });
                return;
            }

            tenantContext.SetTenant(user.TenantId);
        }

        await _next(context);
    }
}
