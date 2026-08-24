using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace KhaiKang.Api.IntegrationTests.Identity;

public sealed class AiAccountTokenEndpointsTests
{
    [Fact]
    public async Task Owner_CanCreateAiAccountAndUseBearerTokenWithoutCsrf()
    {
        using var context = await AuthenticatedApiTestContext.CreateAsync();

        var createResponse = await context.PostJsonAsync(
            "/api/v1/ai-accounts",
            new { username = "ai-development", fixedRole = "development_ai" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadJsonAsync(createResponse);
        var accountId = created.GetProperty("account").GetProperty("id").GetGuid();
        var accountVersion = created.GetProperty("account").GetProperty("version").GetInt32();
        Assert.StartsWith("ai-", created.GetProperty("account").GetProperty("username").GetString());
        Assert.Equal("ai_agent", created.GetProperty("account").GetProperty("accountType").GetString());
        Assert.Equal("development_ai", created.GetProperty("account").GetProperty("fixedRole").GetString());
        Assert.False(created.GetProperty("account").GetProperty("aiApiEnabled").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(created.GetProperty("initialPassword").GetString()));

        using var invalidBearerWithHumanCookie = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/ai-auth/me");
        invalidBearerWithHumanCookie.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "invalid");
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await context.Client.SendAsync(invalidBearerWithHumanCookie)).StatusCode);

        var disabledTokenResponse = await context.PostJsonAsync(
            $"/api/v1/ai-accounts/{accountId}/token",
            new { });
        Assert.Equal(HttpStatusCode.Conflict, disabledTokenResponse.StatusCode);

        var enableResponse = await context.PutJsonAsync(
            $"/api/v1/ai-accounts/{accountId}/api-enabled",
            new { enabled = true, version = accountVersion });
        enableResponse.EnsureSuccessStatusCode();

        var tokenResponse = await context.PostJsonAsync(
            $"/api/v1/ai-accounts/{accountId}/token",
            new { });
        Assert.Equal(HttpStatusCode.Created, tokenResponse.StatusCode);
        var tokenDocument = await ReadJsonAsync(tokenResponse);
        var token = tokenDocument.GetProperty("token").GetString();
        Assert.StartsWith("kkai_", token);

        var viewResponse = await context.Client.GetAsync(
            $"/api/v1/ai-accounts/{accountId}/token");
        viewResponse.EnsureSuccessStatusCode();
        Assert.Equal(token, (await ReadJsonAsync(viewResponse)).GetProperty("token").GetString());

        var recreateResponse = await context.PostJsonAsync(
            $"/api/v1/ai-accounts/{accountId}/token",
            new { });
        recreateResponse.EnsureSuccessStatusCode();
        var recreatedToken = (await ReadJsonAsync(recreateResponse)).GetProperty("token").GetString();
        Assert.NotEqual(token, recreatedToken);

        using var oldBearerClient = context.CreateClient();
        oldBearerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await oldBearerClient.GetAsync("/api/v1/ai-auth/me")).StatusCode);

        using var bearerClient = context.CreateClient();
        bearerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            recreatedToken);
        var bearerResponse = await bearerClient.GetAsync("/api/v1/ai-auth/me");
        bearerResponse.EnsureSuccessStatusCode();
        var bearerIdentity = await ReadJsonAsync(bearerResponse);
        Assert.Equal(accountId, bearerIdentity.GetProperty("id").GetGuid());
        Assert.Equal("ai_agent", bearerIdentity.GetProperty("accountType").GetString());
        Assert.Equal("development_ai", bearerIdentity.GetProperty("fixedRole").GetString());
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await bearerClient.GetAsync("/api/v1/ai-accounts")).StatusCode);

        var revokeResponse = await context.DeleteAsync($"/api/v1/ai-accounts/{accountId}/token");
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await bearerClient.GetAsync("/api/v1/ai-auth/me")).StatusCode);

        using var scope = context.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var storedToken = Assert.Single(dbContext.AiAccountTokens);
        Assert.NotEqual(token, storedToken.ProtectedSecret);
        Assert.NotEqual(recreatedToken, storedToken.ProtectedSecret);
        Assert.DoesNotContain(token!, storedToken.ProtectedSecret, StringComparison.Ordinal);
        Assert.DoesNotContain(recreatedToken!, storedToken.ProtectedSecret, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TransferOwner_RevokesTokenAndAdministratorCannotReadPlaintext()
    {
        using var context = await AuthenticatedApiTestContext.CreateAsync();
        var createAiResponse = await context.PostJsonAsync(
            "/api/v1/ai-accounts",
            new { username = "ai-review", fixedRole = "reviewer_ai" });
        createAiResponse.EnsureSuccessStatusCode();
        var createdAi = await ReadJsonAsync(createAiResponse);
        var aiAccountId = createdAi.GetProperty("account").GetProperty("id").GetGuid();
        var aiVersion = createdAi.GetProperty("account").GetProperty("version").GetInt32();
        var aiInitialPassword = createdAi.GetProperty("initialPassword").GetString();

        var enableResponse = await context.PutJsonAsync(
            $"/api/v1/ai-accounts/{aiAccountId}/api-enabled",
            new { enabled = true, version = aiVersion });
        enableResponse.EnsureSuccessStatusCode();
        var tokenResponse = await context.PostJsonAsync(
            $"/api/v1/ai-accounts/{aiAccountId}/token",
            new { });
        tokenResponse.EnsureSuccessStatusCode();
        var originalToken = (await ReadJsonAsync(tokenResponse)).GetProperty("token").GetString();

        var createOwnerResponse = await context.PostJsonAsync(
            "/api/v1/accounts",
            new { username = "new-ai-owner" });
        createOwnerResponse.EnsureSuccessStatusCode();
        var createdOwner = await ReadJsonAsync(createOwnerResponse);
        var newOwnerId = createdOwner.GetProperty("account").GetProperty("id").GetGuid();
        var newOwnerPassword = createdOwner.GetProperty("initialPassword").GetString();

        var transferResponse = await context.PutJsonAsync(
            $"/api/v1/ai-accounts/{aiAccountId}/owner",
            new { ownerAccountId = newOwnerId, version = aiVersion + 1 });
        transferResponse.EnsureSuccessStatusCode();

        var administratorView = await context.Client.GetAsync(
            $"/api/v1/ai-accounts/{aiAccountId}/token");
        Assert.Equal(HttpStatusCode.Forbidden, administratorView.StatusCode);
        var administratorRecreate = await context.PostJsonAsync(
            $"/api/v1/ai-accounts/{aiAccountId}/token",
            new { });
        Assert.Equal(HttpStatusCode.Forbidden, administratorRecreate.StatusCode);

        using var oldBearerClient = context.CreateClient();
        oldBearerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            originalToken);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await oldBearerClient.GetAsync("/api/v1/ai-auth/me")).StatusCode);

        using var ownerClient = context.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(ownerClient, "new-ai-owner", newOwnerPassword!);
        var recreatedResponse = await AuthenticatedApiTestContext.PostJsonAsync(
            ownerClient,
            $"/api/v1/ai-accounts/{aiAccountId}/token",
            new { });
        recreatedResponse.EnsureSuccessStatusCode();
        var recreatedToken = (await ReadJsonAsync(recreatedResponse)).GetProperty("token").GetString();
        Assert.NotEqual(originalToken, recreatedToken);

        var administratorRevoke = await context.DeleteAsync(
            $"/api/v1/ai-accounts/{aiAccountId}/token");
        Assert.Equal(HttpStatusCode.NoContent, administratorRevoke.StatusCode);
        using var revokedBearerClient = context.CreateClient();
        revokedBearerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            recreatedToken);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await revokedBearerClient.GetAsync("/api/v1/ai-auth/me")).StatusCode);

        var ownerRecreateResponse = await AuthenticatedApiTestContext.PostJsonAsync(
            ownerClient,
            $"/api/v1/ai-accounts/{aiAccountId}/token",
            new { });
        ownerRecreateResponse.EnsureSuccessStatusCode();
        var ownerToken = (await ReadJsonAsync(ownerRecreateResponse)).GetProperty("token").GetString();
        using var ownerBearerClient = context.CreateClient();
        ownerBearerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ownerToken);
        (await ownerBearerClient.GetAsync("/api/v1/ai-auth/me")).EnsureSuccessStatusCode();

        using var aiWebClient = context.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            aiWebClient,
            "ai-review",
            aiInitialPassword!);
        (await aiWebClient.GetAsync("/api/v1/auth/me")).EnsureSuccessStatusCode();

        var accounts = await context.Client.GetFromJsonAsync<AccountResponse[]>("/api/v1/accounts");
        var owner = Assert.Single(accounts!, account => account.Id == newOwnerId);
        var disableOwnerResponse = await context.PutJsonAsync(
            $"/api/v1/accounts/{newOwnerId}/status",
            new { status = "disabled", version = owner.Version });
        disableOwnerResponse.EnsureSuccessStatusCode();

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await ownerBearerClient.GetAsync("/api/v1/ai-auth/me")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await aiWebClient.GetAsync("/api/v1/auth/me")).StatusCode);

        using var scope = context.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var auditText = string.Join('|', dbContext.AuditEvents.Select(item =>
            $"{item.EventType}:{item.Outcome}"));
        Assert.Contains("ai_account_owner_transferred:succeeded", auditText);
        Assert.Contains("ai_token_recreated:succeeded", auditText);
        Assert.Contains("ai_token_revoked:succeeded", auditText);
        Assert.DoesNotContain(originalToken!, auditText, StringComparison.Ordinal);
        Assert.DoesNotContain(recreatedToken!, auditText, StringComparison.Ordinal);
        Assert.DoesNotContain(ownerToken!, auditText, StringComparison.Ordinal);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.Clone();
    }
}
