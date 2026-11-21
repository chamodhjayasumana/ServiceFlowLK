using ServiceFlow.Application.Auth;

namespace ServiceFlow.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
