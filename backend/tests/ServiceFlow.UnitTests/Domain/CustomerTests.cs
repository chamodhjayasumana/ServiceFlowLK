using FluentAssertions;
using ServiceFlow.Domain.Entities;

namespace ServiceFlow.UnitTests.Domain;

public class CustomerTests
{
    [Fact]
    public void Create_AssignsServerTenantId_AndNormalizesPhone()
    {
        var tenantId = Guid.NewGuid();

        var customer = Customer.Create(tenantId, "Nimal Perera", "0771234567", "nimal@example.com");

        customer.TenantId.Should().Be(tenantId);
        customer.FullName.Should().Be("Nimal Perera");
        customer.Phone.Should().Be("+94771234567");
        customer.Email.Should().Be("nimal@example.com");
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_RejectsEmptyTenantId()
    {
        var act = () => Customer.Create(Guid.Empty, "Nimal Perera", "0771234567");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
    }

    [Theory]
    [InlineData("0771234567", "+94771234567")]
    [InlineData("+94771234567", "+94771234567")]
    [InlineData("94771234567", "+94771234567")]
    public void NormalizePhone_SupportsSriLankanFormats(string input, string expected)
    {
        Customer.NormalizePhone(input).Should().Be(expected);
    }
}
