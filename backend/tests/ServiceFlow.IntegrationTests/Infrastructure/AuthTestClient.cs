using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ServiceFlow.Application.Auth;
using ServiceFlow.Application.Tenants;
using ServiceFlow.Application.Users;

namespace ServiceFlow.IntegrationTests.Infrastructure;

public static class AuthTestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<RegisterTenantResult> RegisterTenantAsync(
        HttpClient client,
        string slug,
        string ownerEmail,
        string password = "OwnerPass1!")
    {
        var response = await client.PostAsJsonAsync("/api/v1/tenants/register", new RegisterTenantCommand(
            BusinessName: $"Shop {slug}",
            Slug: slug,
            ContactEmail: ownerEmail,
            ContactPhone: "0771234567",
            AddressLine1: "Colombo",
            City: "Colombo",
            OwnerDisplayName: "Owner User",
            OwnerEmail: ownerEmail,
            OwnerPassword: password));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<RegisterTenantResult>(JsonOptions);
        return payload!;
    }

    public static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        return await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
    }

    public static async Task LoginSuccessfullyAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await LoginAsync(client, email, password);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await AttachCsrfHeaderAsync(client);
    }

    public static async Task AttachCsrfHeaderAsync(HttpClient client)
    {
        var csrfResponse = await client.GetAsync("/api/v1/auth/csrf");
        csrfResponse.EnsureSuccessStatusCode();
        var csrf = await csrfResponse.Content.ReadFromJsonAsync<CsrfTokenDto>(JsonOptions);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", csrf!.Token);
    }

    public static async Task<CurrentUserDto> MeAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/me");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CurrentUserDto>(JsonOptions))!;
    }

    public static async Task<InviteEmployeeResult> InviteAsync(
        HttpClient client,
        string email,
        string roleName)
    {
        await AttachCsrfHeaderAsync(client);
        var response = await client.PostAsJsonAsync("/api/v1/users/invitations", new InviteEmployeeCommand(email, roleName));
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Invite failed: {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<InviteEmployeeResult>(JsonOptions))!;
    }

    private static void ShouldBe(this HttpStatusCode actual, HttpStatusCode expected)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
        }
    }

    private sealed record CsrfTokenDto(string Token);
}
