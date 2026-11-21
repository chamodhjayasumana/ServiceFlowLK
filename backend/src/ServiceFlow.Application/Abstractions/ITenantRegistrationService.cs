using ServiceFlow.Application.Tenants;

namespace ServiceFlow.Application.Abstractions;

public interface ITenantRegistrationService
{
    Task<RegisterTenantResult> RegisterAsync(RegisterTenantCommand command, CancellationToken cancellationToken = default);
}
