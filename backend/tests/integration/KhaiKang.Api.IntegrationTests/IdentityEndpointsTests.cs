using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IdentityEndpointsTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

    [Fact]
    public async Task AuthenticationFlow_InitializesAdminAndRevokesLoggedOutSession()
    {
        var initialStatus = await _client.GetFromJsonAsync<SetupStatusResponse>(
            "/api/v1/setup/status");
        Assert.NotNull(initialStatus);
        Assert.True(initialStatus.RequiresInitialization);

        var anonymousCsrfToken = await GetCsrfTokenAsync();
        var initializeResponse = await PostAsync(
            "/api/v1/setup/initialize",
            content: null,
            anonymousCsrfToken);
        initializeResponse.EnsureSuccessStatusCode();
        var credentials = await initializeResponse.Content.ReadFromJsonAsync<InitializeAdminResponse>();
        Assert.NotNull(credentials);
        Assert.Equal("admin", credentials.Username);
        Assert.True(credentials.InitialPassword.Length >= 12);

        var secondInitializeResponse = await PostAsync(
            "/api/v1/setup/initialize",
            content: null,
            anonymousCsrfToken);
        Assert.Equal(HttpStatusCode.Conflict, secondInitializeResponse.StatusCode);

        var invalidLoginResponse = await PostAsync(
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest("admin", "incorrect-password", false)),
            anonymousCsrfToken);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidLoginResponse.StatusCode);
        var invalidLoginProblem = await invalidLoginResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(invalidLoginProblem);
        Assert.True(invalidLoginProblem.Extensions.TryGetValue("code", out var code));
        Assert.Equal("invalid_credentials", code?.ToString());
        Assert.False(invalidLoginProblem.Extensions.ContainsKey("traceId"));

        var loginResponse = await PostAsync(
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest("admin", credentials.InitialPassword, false)),
            anonymousCsrfToken);
        loginResponse.EnsureSuccessStatusCode();
        var authenticatedUser = await loginResponse.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        Assert.NotNull(authenticatedUser);
        Assert.True(authenticatedUser.MustChangePassword);
        Assert.Contains("System Admin", authenticatedUser.SystemRoles);
        Assert.Contains("account.read", authenticatedUser.SystemPermissions);
        Assert.Contains("project.create", authenticatedUser.SystemPermissions);
        Assert.DoesNotContain("project.read", authenticatedUser.SystemPermissions);

        var currentUser = await _client.GetFromJsonAsync<AuthenticatedUserResponse>(
            "/api/v1/auth/me");
        Assert.NotNull(currentUser);
        Assert.Equal(authenticatedUser.Id, currentUser.Id);

        var authenticatedCsrfToken = await GetCsrfTokenAsync();
        var changePasswordResponse = await PostAsync(
            "/api/v1/auth/password",
            JsonContent.Create(new ChangePasswordRequest(
                credentials.InitialPassword,
                "a-secure-new-password")),
            authenticatedCsrfToken);
        Assert.Equal(HttpStatusCode.NoContent, changePasswordResponse.StatusCode);

        var logoutResponse = await PostAsync(
            "/api/v1/auth/logout",
            content: null,
            authenticatedCsrfToken);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var loggedOutResponse = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOutResponse.StatusCode);
    }

    private async Task<string> GetCsrfTokenAsync()
    {
        var response = await _client.GetFromJsonAsync<CsrfTokenResponse>(
            "/api/v1/auth/csrf-token");
        Assert.NotNull(response);
        return response.Token;
    }

    private async Task<HttpResponseMessage> PostAsync(
        string path,
        HttpContent? content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content,
        };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await _client.SendAsync(request);
    }
}
