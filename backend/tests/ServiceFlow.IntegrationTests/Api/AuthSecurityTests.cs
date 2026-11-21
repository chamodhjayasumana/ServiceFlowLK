using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiceFlow.Application.Tenants;
using ServiceFlow.Application.Users;
using ServiceFlow.Domain.Constants;
using ServiceFlow.Domain.Enums;
using ServiceFlow.Infrastructure.Identity;
using ServiceFlow.Infrastructure.Persistence;
using ServiceFlow.IntegrationTests.Infrastructure;

namespace ServiceFlow.IntegrationTests.Api;

public class AuthSecurityTests : IClassFixture<ServiceFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly ServiceFlowWebApplicationFactory _factory;

    public AuthSecurityTests(ServiceFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_CreatesExactlyOneTenantAndOwner()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var slug = $"shop-{Guid.NewGuid():N}"[..20];

        var result = await AuthTestClient.RegisterTenantAsync(client, slug, email);

        result.Slug.Should().Be(slug);
        result.OwnerEmail.Should().Be(email);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        db.Tenants.Count(t => t.Slug == slug).Should().Be(1);
        var owners = userManager.Users.Where(u => u.Email == email).ToList();
        owners.Should().ContainSingle();
        (await userManager.IsInRoleAsync(owners[0], AppRoles.Owner)).Should().BeTrue();
        owners[0].TenantId.Should().Be(result.TenantId);
    }

    [Fact]
    public async Task Login_RejectsInvalidCredentials()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var slug = $"shop-{Guid.NewGuid():N}"[..20];
        await AuthTestClient.RegisterTenantAsync(client, slug, email);

        var response = await AuthTestClient.LoginAsync(client, email, "WrongPass1!");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_RequiresAuthentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantA_CannotListTenantB_Employees()
    {
        var clientA = _factory.CreateClient(new() { HandleCookies = true });
        var clientB = _factory.CreateClient(new() { HandleCookies = true });

        var emailA = $"a-{Guid.NewGuid():N}@example.com";
        var emailB = $"b-{Guid.NewGuid():N}@example.com";
        var slugA = $"a-{Guid.NewGuid():N}"[..18];
        var slugB = $"b-{Guid.NewGuid():N}"[..18];

        await AuthTestClient.RegisterTenantAsync(clientA, slugA, emailA);
        await AuthTestClient.RegisterTenantAsync(clientB, slugB, emailB);

        await AuthTestClient.LoginSuccessfullyAsync(clientA, emailA, "OwnerPass1!");
        await AuthTestClient.LoginSuccessfullyAsync(clientB, emailB, "OwnerPass1!");

        await AuthTestClient.InviteAsync(clientB, $"tech-{Guid.NewGuid():N}@example.com", AppRoles.Technician);

        var usersResponse = await clientA.GetAsync("/api/v1/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<EmployeeListItemDto>>();

        users.Should().NotBeNull();
        users!.Should().ContainSingle(u => u.Email == emailA);
        users.Should().NotContain(u => u.Email == emailB);
        users.Should().OnlyContain(u => u.Email == emailA);
    }

    [Fact]
    public async Task Invite_RejectsOwnerRole_PrivilegeEscalation()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var slug = $"shop-{Guid.NewGuid():N}"[..20];
        await AuthTestClient.RegisterTenantAsync(client, slug, email);
        await AuthTestClient.LoginSuccessfullyAsync(client, email, "OwnerPass1!");
        await AuthTestClient.AttachCsrfHeaderAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1/users/invitations",
            new InviteEmployeeCommand($"x-{Guid.NewGuid():N}@example.com", AppRoles.Owner));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DisabledUser_CannotLogin()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var slug = $"shop-{Guid.NewGuid():N}"[..20];
        await AuthTestClient.RegisterTenantAsync(client, slug, email);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            user!.IsActive = false;
            await userManager.UpdateAsync(user);
        }

        var response = await AuthTestClient.LoginAsync(client, email, "OwnerPass1!");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuspendedTenant_CannotLogin()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var slug = $"shop-{Guid.NewGuid():N}"[..20];
        var registered = await AuthTestClient.RegisterTenantAsync(client, slug, email);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tenant = await db.Tenants.FindAsync(registered.TenantId);
            tenant!.Suspend();
            await db.SaveChangesAsync();
        }

        var response = await AuthTestClient.LoginAsync(client, email, "OwnerPass1!");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MutatingRequest_WithoutCsrf_IsRejected()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var slug = $"shop-{Guid.NewGuid():N}"[..20];
        await AuthTestClient.RegisterTenantAsync(client, slug, email);
        var login = await AuthTestClient.LoginAsync(client, email, "OwnerPass1!");
        login.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Authenticated cookie present, but no CSRF header.
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        var response = await client.PostAsJsonAsync(
            "/api/v1/users/invitations",
            new InviteEmployeeCommand($"y-{Guid.NewGuid():N}@example.com", AppRoles.Technician));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_InvalidatesSession()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var slug = $"shop-{Guid.NewGuid():N}"[..20];
        await AuthTestClient.RegisterTenantAsync(client, slug, email);
        await AuthTestClient.LoginSuccessfullyAsync(client, email, "OwnerPass1!");

        (await client.GetAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthTestClient.AttachCsrfHeaderAsync(client);
        var logout = await client.PostAsync("/api/v1/auth/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.GetAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DuplicateSlug_IsRejected()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true });
        var slug = $"dup-{Guid.NewGuid():N}"[..18];
        await AuthTestClient.RegisterTenantAsync(client, slug, $"o1-{Guid.NewGuid():N}@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/tenants/register", new RegisterTenantCommand(
            "Other Shop",
            slug,
            null,
            null,
            null,
            null,
            "Owner Two",
            $"o2-{Guid.NewGuid():N}@example.com",
            "OwnerPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
