using FluentValidation;

namespace ServiceFlow.Application.Auth;

public sealed record LoginCommand(string Email, string Password);

public sealed record AuthResult(bool Succeeded, string? Error = null);

public sealed record CurrentUserDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string TenantName,
    string TenantSlug,
    string TenantStatus);

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
