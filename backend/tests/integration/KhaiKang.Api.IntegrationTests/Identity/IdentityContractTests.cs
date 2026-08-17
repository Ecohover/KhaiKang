using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KhaiKang.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IdentityContractTests
{
    [Fact]
    public async Task Requests_RejectMissingOrNullRequiredProperties()
    {
        using var context = await AuthenticatedApiTestContext.CreateAsync();
        var accountId = Guid.NewGuid();
        (HttpMethod Method, string Path, string Json)[] invalidRequests =
        [
            (HttpMethod.Post, "/api/v1/auth/login", """{"password":"secret","rememberMe":false}"""),
            (HttpMethod.Post, "/api/v1/auth/login", """{"username":"admin","rememberMe":false}"""),
            (HttpMethod.Post, "/api/v1/auth/login", """{"username":"admin","password":"secret"}"""),
            (HttpMethod.Post, "/api/v1/auth/login", """{"username":null,"password":"secret","rememberMe":false}"""),
            (HttpMethod.Post, "/api/v1/auth/login", """{"username":"admin","password":null,"rememberMe":false}"""),
            (HttpMethod.Post, "/api/v1/auth/login", """{"username":"admin","password":"secret","rememberMe":null}"""),
            (HttpMethod.Post, "/api/v1/auth/password", """{"newPassword":"new-password"}"""),
            (HttpMethod.Post, "/api/v1/auth/password", """{"currentPassword":"old-password"}"""),
            (HttpMethod.Post, "/api/v1/auth/password", """{"currentPassword":null,"newPassword":"new-password"}"""),
            (HttpMethod.Post, "/api/v1/auth/password", """{"currentPassword":"old-password","newPassword":null}"""),
            (HttpMethod.Post, "/api/v1/accounts", "{}"),
            (HttpMethod.Post, "/api/v1/accounts", """{"username":null}"""),
            (HttpMethod.Put, $"/api/v1/accounts/{accountId}", """{"version":1}"""),
            (HttpMethod.Put, $"/api/v1/accounts/{accountId}", """{"username":"reviewer"}"""),
            (HttpMethod.Put, $"/api/v1/accounts/{accountId}", """{"username":null,"version":1}"""),
            (HttpMethod.Put, $"/api/v1/accounts/{accountId}", """{"username":"reviewer","version":null}"""),
            (HttpMethod.Put, $"/api/v1/accounts/{accountId}/status", """{"version":1}"""),
            (HttpMethod.Put, $"/api/v1/accounts/{accountId}/status", """{"status":"suspended"}"""),
            (HttpMethod.Put, $"/api/v1/accounts/{accountId}/status", """{"status":null,"version":1}"""),
            (HttpMethod.Put, $"/api/v1/accounts/{accountId}/status", """{"status":"suspended","version":null}"""),
        ];

        foreach (var invalidRequest in invalidRequests)
        {
            using var response = await SendRawJsonAsync(
                context.Client,
                invalidRequest.Method,
                invalidRequest.Path,
                invalidRequest.Json);

            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for {invalidRequest.Method} {invalidRequest.Path} with {invalidRequest.Json}, " +
                $"but received {(int)response.StatusCode}.");
        }
    }

    [Fact]
    public async Task Responses_PreserveTheirCanonicalJsonProperties()
    {
        using var factory = new ApiIntegrationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

        using var setupDocument = await GetJsonDocumentAsync(client, "/api/v1/setup/status");
        AssertPropertyNames(setupDocument.RootElement, "requiresInitialization");

        using var csrfDocument = await GetJsonDocumentAsync(client, "/api/v1/auth/csrf-token");
        AssertPropertyNames(csrfDocument.RootElement, "token");
        var csrfToken = csrfDocument.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(csrfToken));

        using var initializeResponse = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/setup/initialize",
            content: null,
            csrfToken!);
        initializeResponse.EnsureSuccessStatusCode();
        using var initializeDocument = await ReadJsonDocumentAsync(initializeResponse);
        AssertPropertyNames(initializeDocument.RootElement, "initialPassword", "username");

        using var loginResponse = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest
            {
                Username = initializeDocument.RootElement.GetProperty("username").GetString()!,
                Password = initializeDocument.RootElement.GetProperty("initialPassword").GetString()!,
                RememberMe = false,
            }),
            csrfToken!);
        loginResponse.EnsureSuccessStatusCode();
        using var authenticatedUserDocument = await ReadJsonDocumentAsync(loginResponse);
        AssertPropertyNames(
            authenticatedUserDocument.RootElement,
            "id",
            "mustChangePassword",
            "systemPermissions",
            "systemRoles",
            "username");

        var authenticatedCsrfToken = await AuthenticatedApiTestContext.GetCsrfTokenAsync(client);
        using var createResponse = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/accounts",
            JsonContent.Create(new CreateAccountRequest
            {
                Username = "contract.reviewer",
            }),
            authenticatedCsrfToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createDocument = await ReadJsonDocumentAsync(createResponse);
        AssertPropertyNames(createDocument.RootElement, "account", "initialPassword");

        var account = createDocument.RootElement.GetProperty("account");
        AssertPropertyNames(
            account,
            "accountType",
            "createdAt",
            "id",
            "lastLoginAt",
            "mustChangePassword",
            "status",
            "systemRoles",
            "updatedAt",
            "username",
            "version");
        Assert.Equal(JsonValueKind.Null, account.GetProperty("lastLoginAt").ValueKind);
    }

    private static async Task<HttpResponseMessage> SendRawJsonAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string json)
    {
        return await SendAsync(
            client,
            method,
            path,
            new StringContent(json, Encoding.UTF8, "application/json"),
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(client));
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

    private static async Task<JsonDocument> GetJsonDocumentAsync(
        HttpClient client,
        string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await ReadJsonDocumentAsync(response);
    }

    private static async Task<JsonDocument> ReadJsonDocumentAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static void AssertPropertyNames(JsonElement element, params string[] expectedNames)
    {
        var actualNames = element
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedNames.Order(StringComparer.Ordinal), actualNames);
    }
}
