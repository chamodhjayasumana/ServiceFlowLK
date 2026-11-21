using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ServiceFlow.Application.Abstractions;
using ServiceFlow.Application.Authorization;
using ServiceFlow.Application.Tenants;

namespace ServiceFlow.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantRegistrationService _registrationService;
    private readonly IValidator<RegisterTenantCommand> _validator;
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public TenantsController(
        ITenantRegistrationService registrationService,
        IValidator<RegisterTenantCommand> validator,
        IApplicationDbContext dbContext,
        ITenantContext tenantContext)
    {
        _registrationService = registrationService;
        _validator = validator;
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(RegisterTenantResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterTenantResult>> Register(
        [FromBody] RegisterTenantCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        var result = await _registrationService.RegisterAsync(command, cancellationToken);
        return Created($"/api/v1/tenants/current", result);
    }

    [Authorize(Policy = Permissions.TenantView)]
    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentTenantResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentTenantResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        if (!_tenantContext.HasTenant)
        {
            return Unauthorized();
        }

        var tenant = await _dbContext.Tenants.FindAsync([_tenantContext.TenantId!.Value], cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(new CurrentTenantResponse(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Status.ToString(),
            tenant.TimeZoneId));
    }
}

public sealed record CurrentTenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    string TimeZoneId);
