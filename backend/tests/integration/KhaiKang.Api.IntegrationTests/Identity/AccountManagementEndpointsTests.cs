using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class AccountManagementEndpointsTests(ApiIntegrationTestFactory factory)
    : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly HttpClient _adminClient = CreateClient(factory);

    [Fact]
    public async Task AccountManagement_CreatesListsAndChangesAccountStatus()
    {
        var anonymousCsrfToken = await GetCsrfTokenAsync(_adminClient);
        var initializeResponse = await PostAsync(
            _adminClient,
            "/api/v1/setup/initialize",
            content: null,
            anonymousCsrfToken);
        initializeResponse.EnsureSuccessStatusCode();
        var adminCredentials = await initializeResponse.Content
            .ReadFromJsonAsync<InitializeAdminResponse>();
        Assert.NotNull(adminCredentials);

        var loginResponse = await PostAsync(
            _adminClient,
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest(
                adminCredentials.Username,
                adminCredentials.InitialPassword,
                false)),
            anonymousCsrfToken);
        loginResponse.EnsureSuccessStatusCode();
        var admin = await loginResponse.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        Assert.NotNull(admin);

        var adminCsrfToken = await GetCsrfTokenAsync(_adminClient);
        var createResponse = await PostAsync(
            _adminClient,
            "/api/v1/accounts",
            JsonContent.Create(new CreateAccountRequest("reviewer.one")),
            adminCsrfToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAccountResponse>();
        Assert.NotNull(created);
        Assert.Equal("reviewer.one", created.Account.Username);
        Assert.Equal("active", created.Account.Status);
        Assert.Equal(["User"], created.Account.SystemRoles);
        Assert.True(created.Account.MustChangePassword);
        Assert.True(created.InitialPassword.Length >= 12);

        var duplicateResponse = await PostAsync(
            _adminClient,
            "/api/v1/accounts",
            JsonContent.Create(new CreateAccountRequest("REVIEWER.ONE")),
            adminCsrfToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        await AssertProblemCodeAsync(duplicateResponse, "username_conflict");

        var updateResponse = await PutAsync(
            _adminClient,
            $"/api/v1/accounts/{created.Account.Id}",
            JsonContent.Create(new UpdateAccountRequest("reviewer.two", created.Account.Version)),
            adminCsrfToken);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(updated);
        Assert.Equal("reviewer.two", updated.Username);

        var conflictingUpdateResponse = await PutAsync(
            _adminClient,
            $"/api/v1/accounts/{updated.Id}",
            JsonContent.Create(new UpdateAccountRequest("admin", updated.Version)),
            adminCsrfToken);
        Assert.Equal(HttpStatusCode.Conflict, conflictingUpdateResponse.StatusCode);
        await AssertProblemCodeAsync(conflictingUpdateResponse, "username_conflict");

        using var userClient = CreateClient(factory);
        var userCsrfToken = await GetCsrfTokenAsync(userClient);
        var userLoginResponse = await PostAsync(
            userClient,
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest(
                updated.Username,
                created.InitialPassword,
                false)),
            userCsrfToken);
        userLoginResponse.EnsureSuccessStatusCode();

        var forbiddenListResponse = await userClient.GetAsync("/api/v1/accounts");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenListResponse.StatusCode);

        var accounts = await _adminClient.GetFromJsonAsync<AccountResponse[]>("/api/v1/accounts");
        Assert.NotNull(accounts);
        Assert.Equal(2, accounts.Length);
        var currentUserAccount = Assert.Single(
            accounts,
            account => account.Id == updated.Id);
        var currentAdminAccount = Assert.Single(accounts, account => account.Id == admin.Id);

        var selfUpdateResponse = await PutAsync(
            _adminClient,
            $"/api/v1/accounts/{admin.Id}",
            JsonContent.Create(new UpdateAccountRequest(
                "new-admin",
                currentAdminAccount.Version)),
            adminCsrfToken);
        Assert.Equal(HttpStatusCode.Conflict, selfUpdateResponse.StatusCode);
        await AssertProblemCodeAsync(selfUpdateResponse, "cannot_update_own_account");

        var selfStatusResponse = await PutAsync(
            _adminClient,
            $"/api/v1/accounts/{admin.Id}/status",
            JsonContent.Create(new UpdateAccountStatusRequest(
                "suspended",
                currentAdminAccount.Version)),
            adminCsrfToken);
        Assert.Equal(HttpStatusCode.Conflict, selfStatusResponse.StatusCode);
        await AssertProblemCodeAsync(selfStatusResponse, "cannot_change_own_status");

        var suspendResponse = await PutAsync(
            _adminClient,
            $"/api/v1/accounts/{currentUserAccount.Id}/status",
            JsonContent.Create(new UpdateAccountStatusRequest(
                "suspended",
                currentUserAccount.Version)),
            adminCsrfToken);
        suspendResponse.EnsureSuccessStatusCode();
        var suspended = await suspendResponse.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(suspended);
        Assert.Equal("suspended", suspended.Status);

        var revokedSessionResponse = await userClient.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedSessionResponse.StatusCode);

        var restoreResponse = await PutAsync(
            _adminClient,
            $"/api/v1/accounts/{suspended.Id}/status",
            JsonContent.Create(new UpdateAccountStatusRequest("active", suspended.Version)),
            adminCsrfToken);
        restoreResponse.EnsureSuccessStatusCode();
        var restored = await restoreResponse.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(restored);
        Assert.Equal("active", restored.Status);
    }

    private static HttpClient CreateClient(ApiIntegrationTestFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<CsrfTokenResponse>(
            "/api/v1/auth/csrf-token");
        Assert.NotNull(response);
        return response.Token;
    }

    private static Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string path,
        HttpContent? content,
        string csrfToken)
    {
        return SendAsync(client, HttpMethod.Post, path, content, csrfToken);
    }

    private static Task<HttpResponseMessage> PutAsync(
        HttpClient client,
        string path,
        HttpContent content,
        string csrfToken)
    {
        return SendAsync(client, HttpMethod.Put, path, content, csrfToken);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        HttpContent? content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = content,
        };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Extensions.TryGetValue("code", out var code));
        Assert.Equal(expectedCode, code?.ToString());
    }
}
