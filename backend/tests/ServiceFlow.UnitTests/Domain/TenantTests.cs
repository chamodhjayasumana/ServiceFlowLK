using FluentAssertions;
using ServiceFlow.Domain.Entities;
using ServiceFlow.Domain.Enums;

namespace ServiceFlow.UnitTests.Domain;

public class TenantTests
{
    [Fact]
    public void Create_NormalizesSlug_AndDefaultsTimezone()
    {
        var tenant = Tenant.Create("Colombo Tech Repair", " Colombo-Tech ");

        tenant.Name.Should().Be("Colombo Tech Repair");
        tenant.Slug.Should().Be("colombo-tech");
        tenant.TimeZoneId.Should().Be("Asia/Colombo");
        tenant.Status.Should().Be(TenantStatus.PendingSetup);
    }

    [Fact]
    public void Activate_SetsActiveStatus()
    {
        var tenant = Tenant.Create("Shop", "shop");

        tenant.Activate();

        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.UpdatedAtUtc.Should().NotBeNull();
    }
}
