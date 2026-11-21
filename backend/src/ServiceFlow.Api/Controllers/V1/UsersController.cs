using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Application.Abstractions;
using ServiceFlow.Application.Authorization;
using ServiceFlow.Application.Users;

namespace ServiceFlow.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IEmployeeInvitationService _invitationService;
    private readonly IValidator<InviteEmployeeCommand> _inviteValidator;
    private readonly IValidator<AcceptInvitationCommand> _acceptValidator;

    public UsersController(
        IEmployeeInvitationService invitationService,
        IValidator<InviteEmployeeCommand> inviteValidator,
        IValidator<AcceptInvitationCommand> acceptValidator)
    {
        _invitationService = invitationService;
        _inviteValidator = inviteValidator;
        _acceptValidator = acceptValidator;
    }

    [Authorize(Policy = Permissions.EmployeesInvite)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeListItemDto>>> List(CancellationToken cancellationToken)
    {
        var employees = await _invitationService.ListEmployeesAsync(cancellationToken);
        return Ok(employees);
    }

    [Authorize(Policy = Permissions.EmployeesInvite)]
    [HttpPost("invitations")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(InviteEmployeeResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<InviteEmployeeResult>> Invite(
        [FromBody] InviteEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _inviteValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        var result = await _invitationService.InviteAsync(command, cancellationToken);
        return Created($"/api/v1/users/invitations/{result.InvitationId}", result);
    }

    [AllowAnonymous]
    [HttpPost("invitations/accept")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(AcceptInvitationResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<AcceptInvitationResult>> Accept(
        [FromBody] AcceptInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _acceptValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        var result = await _invitationService.AcceptAsync(command, cancellationToken);
        return Created($"/api/v1/auth/me", result);
    }
}
