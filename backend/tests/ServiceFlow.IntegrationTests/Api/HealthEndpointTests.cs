using System.Net;
using FluentAssertions;
using ServiceFlow.IntegrationTests.Infrastructure;

namespace ServiceFlow.IntegrationTests.Api;

public class HealthEndpointTests : IClassFixture<ServiceFlowWebApplicationFactory>
{
    private readonly ServiceFlowWebApplicationFactory _factory;

    public HealthEndpointTests(ServiceFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Info_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/info");

        var payload = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: payload);
        payload.Should().Contain("ServiceFlow LK API");
    }
}
