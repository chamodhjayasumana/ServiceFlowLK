using ServiceFlow.Application.Users;

namespace ServiceFlow.Application.Abstractions;

public interface IEmployeeInvitationService
{
    Task<InviteEmployeeResult> InviteAsync(InviteEmployeeCommand command, CancellationToken cancellationToken = default);

    Task<AcceptInvitationResult> AcceptAsync(AcceptInvitationCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeListItemDto>> ListEmployeesAsync(CancellationToken cancellationToken = default);
}
