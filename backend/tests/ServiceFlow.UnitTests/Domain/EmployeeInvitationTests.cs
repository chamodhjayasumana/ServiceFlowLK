using FluentAssertions;
using ServiceFlow.Domain.Entities;
using ServiceFlow.Domain.Enums;

namespace ServiceFlow.UnitTests.Domain;

public class EmployeeInvitationTests
{
    [Fact]
    public void MarkAccepted_Fails_WhenExpired()
    {
        var invitation = EmployeeInvitation.Create(
            Guid.NewGuid(),
            "tech@example.com",
            "Technician",
            "hash",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(5));

        // Force expiry for the assertion without waiting.
        typeof(EmployeeInvitation)
            .GetProperty(nameof(EmployeeInvitation.ExpiresAtUtc))!
            .SetValue(invitation, DateTimeOffset.UtcNow.AddMinutes(-1));

        var act = () => invitation.MarkAccepted(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>();
        invitation.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public void MarkAccepted_Succeeds_WhenPendingAndValid()
    {
        var invitation = EmployeeInvitation.Create(
            Guid.NewGuid(),
            "tech@example.com",
            "Technician",
            "hash",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1));

        invitation.MarkAccepted(DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.AcceptedAtUtc.Should().NotBeNull();
    }
}
