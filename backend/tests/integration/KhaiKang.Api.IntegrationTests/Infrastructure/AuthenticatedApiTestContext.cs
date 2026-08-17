using System.Net.Http.Json;
using KhaiKang.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KhaiKang.Api.IntegrationTests;

internal sealed class AuthenticatedApiTestContext : IDisposable
{
    public const string TemporaryPassword = "Temporary-Pass-123!";

    private AuthenticatedApiTestContext(
        ApiIntegrationTestFactory factory,
        HttpClient client)
    {
        Factory = factory;
        Client = client;
    }

    public ApiIntegrationTestFactory Factory { get; }

    public HttpClient Client { get; }

    public static async Task<AuthenticatedApiTestContext> CreateAsync(
        Action<IServiceCollection>? configureTestServices = null)
    {
        var factory = new ApiIntegrationTestFactory(configureTestServices);
        var client = CreateClient(factory);

        try
        {
            var csrfToken = await GetCsrfTokenAsync(client);
            var initializeResponse = await PostAsync(
                client,
                "/api/v1/setup/initialize",
                content: null,
                csrfToken);
            initializeResponse.EnsureSuccessStatusCode();
            var credentials = await initializeResponse.Content
                .ReadFromJsonAsync<InitializeAdminResponse>();
            Assert.NotNull(credentials);

            await LoginAsync(
                client,
                "admin",
                credentials.InitialPassword);
            return new AuthenticatedApiTestContext(factory, client);
        }
        catch
        {
            client.Dispose();
            factory.Dispose();
            throw;
        }
    }

    public HttpClient CreateClient()
    {
        return CreateClient(Factory);
    }

    public Task<HttpResponseMessage> PostJsonAsync(string path, object content)
    {
        return SendJsonAsync(Client, HttpMethod.Post, path, content);
    }

    public async Task<HttpResponseMessage> PostAsync(
        string path,
        HttpContent? content = null)
    {
        return await PostAsync(
            Client,
            path,
            content,
            await GetCsrfTokenAsync(Client));
    }

    public Task<HttpResponseMessage> PutJsonAsync(string path, object content)
    {
        return SendJsonAsync(Client, HttpMethod.Put, path, content);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        request.Headers.Add("X-XSRF-TOKEN", await GetCsrfTokenAsync(Client));
        return await Client.SendAsync(request);
    }

    public static async Task LoginAsync(
        HttpClient client,
        string username,
        string password)
    {
        var response = await SendJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/login",
            new LoginRequest
            {
                Username = username,
                Password = password,
                RememberMe = false,
            });
        response.EnsureSuccessStatusCode();
    }

    public static Task<HttpResponseMessage> PostJsonAsync(
        HttpClient client,
        string path,
        object content)
    {
        return SendJsonAsync(client, HttpMethod.Post, path, content);
    }

    public static async Task<HttpResponseMessage> SendJsonAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object content)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(content),
        };
        request.Headers.Add("X-XSRF-TOKEN", await GetCsrfTokenAsync(client));
        return await client.SendAsync(request);
    }

    public static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<CsrfTokenResponse>(
            "/api/v1/auth/csrf-token");
        Assert.NotNull(response);
        return response.Token;
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
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

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string path,
        HttpContent? content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content,
        };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }
}
