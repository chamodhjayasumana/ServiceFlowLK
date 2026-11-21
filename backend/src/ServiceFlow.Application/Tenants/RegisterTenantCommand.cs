using FluentValidation;

namespace ServiceFlow.Application.Tenants;

public sealed record RegisterTenantCommand(
    string BusinessName,
    string Slug,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine1,
    string? City,
    string OwnerDisplayName,
    string OwnerEmail,
    string OwnerPassword);

public sealed record RegisterTenantResult(
    Guid TenantId,
    string Slug,
    Guid OwnerUserId,
    string OwnerEmail);

public sealed class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase letters, numbers, and hyphens.");
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.ContactPhone).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));
        RuleFor(x => x.AddressLine1).MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.AddressLine1));
        RuleFor(x => x.City).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.City));
        RuleFor(x => x.OwnerDisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.OwnerPassword)
            .NotEmpty()
            .MinimumLength(10)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a non-alphanumeric character.");
    }
}
