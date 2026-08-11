using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TestWorkspaceProjectEndpointsTests(ApiIntegrationTestFactory factory)
    : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly ApiIntegrationTestFactory _factory = factory;

    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

    [Fact]
    public async Task WorkspaceProjectFlow_EnforcesMembershipVisibilityAndOptimisticConcurrency()
    {
        var csrfToken = await GetCsrfTokenAsync(_client);
        var initializeResponse = await PostAsync(
            _client,
            "/api/v1/setup/initialize",
            content: null,
            csrfToken);
        initializeResponse.EnsureSuccessStatusCode();
        var credentials = await initializeResponse.Content.ReadFromJsonAsync<InitializeAdminResponse>();
        Assert.NotNull(credentials);

        var loginResponse = await PostAsync(
            _client,
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest("admin", credentials.InitialPassword, false)),
            csrfToken);
        loginResponse.EnsureSuccessStatusCode();

        var createProjectResponse = await PostAsync(
            _client,
            "/api/v1/projects",
            JsonContent.Create(new CreateProjectRequest("WEB", "Web Project", null)),
            await GetCsrfTokenAsync(_client));
        Assert.Equal(HttpStatusCode.Created, createProjectResponse.StatusCode);
        var project = await createProjectResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);

        var createHiddenProjectResponse = await PostAsync(
            _client,
            "/api/v1/projects",
            JsonContent.Create(new CreateProjectRequest("OPS", "Operations Project", null)),
            await GetCsrfTokenAsync(_client));
        Assert.Equal(HttpStatusCode.Created, createHiddenProjectResponse.StatusCode);
        var hiddenProject = await createHiddenProjectResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(hiddenProject);

        var createWorkspaceResponse = await PostAsync(
            _client,
            "/api/v1/test-workspaces",
            JsonContent.Create(new CreateTestWorkspaceRequest("Release Verification", "RV", null)),
            await GetCsrfTokenAsync(_client));
        Assert.Equal(HttpStatusCode.Created, createWorkspaceResponse.StatusCode);
        var workspace = await createWorkspaceResponse.Content.ReadFromJsonAsync<TestWorkspaceResponse>();
        Assert.NotNull(workspace);

        var linkResponse = await PostAsync(
            _client,
            $"/api/v1/test-workspaces/{workspace.Id}/projects",
            JsonContent.Create(new LinkTestWorkspaceProjectRequest(project.Id)),
            await GetCsrfTokenAsync(_client));
        Assert.Equal(HttpStatusCode.Created, linkResponse.StatusCode);
        var link = await linkResponse.Content.ReadFromJsonAsync<TestWorkspaceProjectResponse>();
        Assert.NotNull(link);
        Assert.Equal(project.Id, link.ProjectId);
        Assert.Equal("WEB", link.Code);
        Assert.Equal("Web Project", link.Name);
        Assert.Equal("active", link.Status);

        var links = await _client.GetFromJsonAsync<TestWorkspaceProjectResponse[]>(
            $"/api/v1/test-workspaces/{workspace.Id}/projects");
        Assert.NotNull(links);
        Assert.Equal(link.Id, Assert.Single(links).Id);

        var duplicateResponse = await PostAsync(
            _client,
            $"/api/v1/test-workspaces/{workspace.Id}/projects",
            JsonContent.Create(new LinkTestWorkspaceProjectRequest(project.Id)),
            await GetCsrfTokenAsync(_client));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        await _factory.AddActiveAccountAsync("manager");
        var addManagerResponse = await PostAsync(
            _client,
            $"/api/v1/test-workspaces/{workspace.Id}/members",
            JsonContent.Create(new AddTestWorkspaceMemberRequest("manager", "manager")),
            await GetCsrfTokenAsync(_client));
        Assert.Equal(HttpStatusCode.Created, addManagerResponse.StatusCode);

        using var managerClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
            });
        var managerLoginResponse = await PostAsync(
            managerClient,
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest("manager", "Temporary-Pass-123!", false)),
            await GetCsrfTokenAsync(managerClient));
        managerLoginResponse.EnsureSuccessStatusCode();

        var inaccessibleProjectResponse = await PostAsync(
            managerClient,
            $"/api/v1/test-workspaces/{workspace.Id}/projects",
            JsonContent.Create(new LinkTestWorkspaceProjectRequest(hiddenProject.Id)),
            await GetCsrfTokenAsync(managerClient));
        Assert.Equal(HttpStatusCode.NotFound, inaccessibleProjectResponse.StatusCode);

        await _factory.AddActiveAccountAsync("outsider");
        using var outsiderClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
            });
        var outsiderLoginResponse = await PostAsync(
            outsiderClient,
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest("outsider", "Temporary-Pass-123!", false)),
            await GetCsrfTokenAsync(outsiderClient));
        outsiderLoginResponse.EnsureSuccessStatusCode();
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await outsiderClient.GetAsync(
                $"/api/v1/test-workspaces/{workspace.Id}/projects")).StatusCode);

        var staleUnlinkResponse = await DeleteAsync(
            _client,
            $"/api/v1/test-workspaces/{workspace.Id}/projects/{project.Id}?version={link.Version + 1}",
            await GetCsrfTokenAsync(_client));
        Assert.Equal(HttpStatusCode.Conflict, staleUnlinkResponse.StatusCode);

        var unlinkResponse = await DeleteAsync(
            _client,
            $"/api/v1/test-workspaces/{workspace.Id}/projects/{project.Id}?version={link.Version}",
            await GetCsrfTokenAsync(_client));
        Assert.Equal(HttpStatusCode.NoContent, unlinkResponse.StatusCode);

        var linksAfterUnlink = await _client.GetFromJsonAsync<TestWorkspaceProjectResponse[]>(
            $"/api/v1/test-workspaces/{workspace.Id}/projects");
        Assert.NotNull(linksAfterUnlink);
        Assert.Empty(linksAfterUnlink);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<CsrfTokenResponse>(
            "/api/v1/auth/csrf-token");
        Assert.NotNull(response);
        return response.Token;
    }

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string path,
        HttpContent? content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> DeleteAsync(
        HttpClient client,
        string path,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }
}
