using FluentValidation;
using ServiceFlow.Domain.Constants;

namespace ServiceFlow.Application.Users;

public sealed record InviteEmployeeCommand(string Email, string RoleName);

public sealed record InviteEmployeeResult(
    Guid InvitationId,
    string Email,
    string RoleName,
    DateTimeOffset ExpiresAtUtc,
    string InvitationToken);

public sealed record AcceptInvitationCommand(
    string Token,
    string DisplayName,
    string Password);

public sealed record AcceptInvitationResult(
    Guid UserId,
    Guid TenantId,
    string Email,
    string RoleName);

public sealed record EmployeeListItemDto(
    Guid UserId,
    string Email,
    string DisplayName,
    bool IsActive,
    IReadOnlyList<string> Roles);

public sealed class InviteEmployeeCommandValidator : AbstractValidator<InviteEmployeeCommand>
{
    public InviteEmployeeCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.RoleName)
            .NotEmpty()
            .Must(AppRoles.IsAssignableByInvitation)
            .WithMessage("Role cannot be assigned by invitation.");
    }
}

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MinimumLength(32);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a non-alphanumeric character.");
    }
}
