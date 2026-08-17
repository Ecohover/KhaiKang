using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IdentityOutcomeMappingTests
{
    [Fact]
    public async Task CreateAccount_MapsEachReachableOutcome()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();

        var created = await CreateAccountAsync(api, "outcome.create");

        using var duplicateResponse = await api.PostJsonAsync(
            "/api/v1/accounts",
            new CreateAccountRequest
            {
                Username = "OUTCOME.CREATE",
            });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        await AssertProblemCodeAsync(duplicateResponse, "username_conflict");

        using var unconfiguredApi = await AuthenticatedApiTestContext.CreateAsync();
        await RemoveDefaultUserRoleAsync(unconfiguredApi);

        using var unconfiguredResponse = await unconfiguredApi.PostJsonAsync(
            "/api/v1/accounts",
            new CreateAccountRequest
            {
                Username = "outcome.no-role",
            });
        Assert.Equal(HttpStatusCode.InternalServerError, unconfiguredResponse.StatusCode);
        await AssertProblemCodeAsync(unconfiguredResponse, "account_configuration_invalid");

        Assert.Equal("outcome.create", created.Account.Username);
    }

    [Fact]
    public async Task UpdateAccount_MapsEachReachableOutcome()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var account = await CreateAccountAsync(api, "outcome.update");
        var conflictingAccount = await CreateAccountAsync(api, "outcome.existing");
        var admin = await GetAccountAsync(api, "admin");

        using var missingResponse = await api.PutJsonAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}",
            new UpdateAccountRequest
            {
                Username = "outcome.missing",
                Version = 1,
            });
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        await AssertProblemCodeAsync(missingResponse, "account_not_found");

        using var duplicateResponse = await api.PutJsonAsync(
            $"/api/v1/accounts/{account.Account.Id}",
            new UpdateAccountRequest
            {
                Username = conflictingAccount.Account.Username,
                Version = account.Account.Version,
            });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        await AssertProblemCodeAsync(duplicateResponse, "username_conflict");

        using var selfResponse = await api.PutJsonAsync(
            $"/api/v1/accounts/{admin.Id}",
            new UpdateAccountRequest
            {
                Username = "outcome.admin",
                Version = admin.Version,
            });
        Assert.Equal(HttpStatusCode.Conflict, selfResponse.StatusCode);
        await AssertProblemCodeAsync(selfResponse, "cannot_update_own_account");

        using var updateResponse = await api.PutJsonAsync(
            $"/api/v1/accounts/{account.Account.Id}",
            new UpdateAccountRequest
            {
                Username = "outcome.updated",
                Version = account.Account.Version,
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = Assert.IsType<AccountResponse>(
            await updateResponse.Content.ReadFromJsonAsync<AccountResponse>());

        using var staleResponse = await api.PutJsonAsync(
            $"/api/v1/accounts/{updated.Id}",
            new UpdateAccountRequest
            {
                Username = "outcome.stale",
                Version = account.Account.Version,
            });
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        await AssertProblemCodeAsync(staleResponse, "account_version_conflict");
    }

    [Fact]
    public async Task UpdateAccountStatus_MapsEachReachableOutcome()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var account = await CreateAccountAsync(api, "outcome.status");
        var admin = await GetAccountAsync(api, "admin");

        using var missingResponse = await api.PutJsonAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/status",
            new UpdateAccountStatusRequest
            {
                Status = "suspended",
                Version = 1,
            });
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        await AssertProblemCodeAsync(missingResponse, "account_not_found");

        using var selfResponse = await api.PutJsonAsync(
            $"/api/v1/accounts/{admin.Id}/status",
            new UpdateAccountStatusRequest
            {
                Status = "suspended",
                Version = admin.Version,
            });
        Assert.Equal(HttpStatusCode.Conflict, selfResponse.StatusCode);
        await AssertProblemCodeAsync(selfResponse, "cannot_change_own_status");

        using var updateResponse = await api.PutJsonAsync(
            $"/api/v1/accounts/{account.Account.Id}/status",
            new UpdateAccountStatusRequest
            {
                Status = "suspended",
                Version = account.Account.Version,
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = Assert.IsType<AccountResponse>(
            await updateResponse.Content.ReadFromJsonAsync<AccountResponse>());

        using var staleResponse = await api.PutJsonAsync(
            $"/api/v1/accounts/{updated.Id}/status",
            new UpdateAccountStatusRequest
            {
                Status = "active",
                Version = account.Account.Version,
            });
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        await AssertProblemCodeAsync(staleResponse, "account_version_conflict");
    }

    [Fact]
    public async Task Authentication_MapsInvalidCredentialsAndPasswordFailures()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();

        using var invalidLoginResponse = await api.PostJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest
            {
                Username = "admin",
                Password = "wrong-password",
                RememberMe = false,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, invalidLoginResponse.StatusCode);
        await AssertProblemCodeAsync(invalidLoginResponse, "invalid_credentials");

        using var invalidPasswordResponse = await api.PostJsonAsync(
            "/api/v1/auth/password",
            new ChangePasswordRequest
            {
                CurrentPassword = "wrong-password",
                NewPassword = "A-valid-new-password",
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPasswordResponse.StatusCode);
        await AssertProblemCodeAsync(invalidPasswordResponse, "invalid_current_password");

        using var shortPasswordResponse = await api.PostJsonAsync(
            "/api/v1/auth/password",
            new ChangePasswordRequest
            {
                CurrentPassword = AuthenticatedApiTestContext.TemporaryPassword,
                NewPassword = "short",
            });
        Assert.Equal(HttpStatusCode.BadRequest, shortPasswordResponse.StatusCode);
    }

    private static async Task<CreateAccountResponse> CreateAccountAsync(
        AuthenticatedApiTestContext api,
        string username)
    {
        using var response = await api.PostJsonAsync(
            "/api/v1/accounts",
            new CreateAccountRequest
            {
                Username = username,
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<CreateAccountResponse>(
            await response.Content.ReadFromJsonAsync<CreateAccountResponse>());
    }

    private static async Task<AccountResponse> GetAccountAsync(
        AuthenticatedApiTestContext api,
        string username)
    {
        var accounts = await api.Client.GetFromJsonAsync<AccountResponse[]>("/api/v1/accounts");
        Assert.NotNull(accounts);
        return Assert.Single(accounts, account => account.Username == username);
    }

    private static async Task RemoveDefaultUserRoleAsync(AuthenticatedApiTestContext api)
    {
        using var scope = api.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var role = await dbContext.SystemRoles.SingleAsync(item => item.NormalizedName == "USER");
        dbContext.SystemRoles.Remove(role);
        await dbContext.SaveChangesAsync();
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, document.RootElement.GetProperty("code").GetString());
    }
}
