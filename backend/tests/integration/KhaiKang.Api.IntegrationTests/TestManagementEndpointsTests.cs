using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TestManagementEndpointsTests(IdentityApiFactory factory)
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
    public async Task TestManagement_CreateWorkspaceSuiteAndCase_PersistsOrderedSteps()
    {
        var csrfToken = await GetCsrfTokenAsync();
        var initializeResponse = await PostAsync(
            "/api/v1/setup/initialize",
            content: null,
            csrfToken);
        initializeResponse.EnsureSuccessStatusCode();
        var credentials = await initializeResponse.Content.ReadFromJsonAsync<InitializeAdminResponse>();
        Assert.NotNull(credentials);

        var loginResponse = await PostAsync(
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest("admin", credentials.InitialPassword, false)),
            csrfToken);
        loginResponse.EnsureSuccessStatusCode();

        var explicitResponse = await CreateWorkspaceAsync("Regression", "qa");
        Assert.Equal(HttpStatusCode.Created, explicitResponse.StatusCode);
        var explicitWorkspace =
            await explicitResponse.Content.ReadFromJsonAsync<TestWorkspaceResponse>();
        Assert.NotNull(explicitWorkspace);
        Assert.Equal("QA", explicitWorkspace.Prefix);

        var generatedResponse = await CreateWorkspaceAsync("Web Checkout", null);
        Assert.Equal(HttpStatusCode.Created, generatedResponse.StatusCode);
        var generatedWorkspace =
            await generatedResponse.Content.ReadFromJsonAsync<TestWorkspaceResponse>();
        Assert.NotNull(generatedWorkspace);
        Assert.Equal("WEBCHE", generatedWorkspace.Prefix);

        var generatedConflictResponse = await CreateWorkspaceAsync("Web Checkers", null);
        Assert.Equal(HttpStatusCode.Created, generatedConflictResponse.StatusCode);
        var generatedConflictWorkspace =
            await generatedConflictResponse.Content.ReadFromJsonAsync<TestWorkspaceResponse>();
        Assert.NotNull(generatedConflictWorkspace);
        Assert.Equal("WEBCHE2", generatedConflictWorkspace.Prefix);

        var duplicateResponse = await CreateWorkspaceAsync("API Regression", "QA");
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var invalidResponse = await CreateWorkspaceAsync("Invalid Prefix", "1bad");
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        var suiteResponse = await PostAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/suites",
            JsonContent.Create(new CreateTestSuiteRequest(
                null,
                "Authentication",
                "Authentication scenarios",
                1)),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, suiteResponse.StatusCode);
        var suite = await suiteResponse.Content.ReadFromJsonAsync<TestSuiteResponse>();
        Assert.NotNull(suite);

        var createCaseResponse = await PostAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases",
            JsonContent.Create(new CreateTestCaseRequest(
                suite.Id,
                "Sign in with valid credentials",
                "Verifies the normal sign-in flow.",
                "An active account exists.",
                "The user reaches the home page.",
                1,
                [
                    new("Open the sign-in page.", "The sign-in form is displayed."),
                    new("Submit valid credentials.", "The home page is displayed."),
                ])),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, createCaseResponse.StatusCode);
        var testCase = await createCaseResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(testCase);
        Assert.Equal(suite.Id, testCase.SuiteId);
        Assert.Collection(
            testCase.Steps,
            step => Assert.Equal(1, step.StepNo),
            step => Assert.Equal(2, step.StepNo));

        var caseListResponse = await _client.GetAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases?suiteId={suite.Id}");
        caseListResponse.EnsureSuccessStatusCode();
        var cases = await caseListResponse.Content.ReadFromJsonAsync<TestCaseResponse[]>();
        var listedCase = Assert.Single(Assert.IsType<TestCaseResponse[]>(cases));
        Assert.Equal(testCase.Id, listedCase.Id);
        Assert.Equal(2, listedCase.Steps.Count);

        var getCaseResponse = await _client.GetAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases/{testCase.Id}");
        getCaseResponse.EnsureSuccessStatusCode();
        var fetchedCase = await getCaseResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(fetchedCase);
        Assert.Equal(testCase.Id, fetchedCase.Id);

        var updateCaseRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases/{testCase.Id}")
        {
            Content = JsonContent.Create(new UpdateTestCaseRequest(
                suite.Id,
                "Sign in with valid credentials - Updated",
                "Updated description.",
                "Updated preconditions.",
                "Updated expected result.",
                2,
                "active",
                testCase.Version,
                [
                    new("Updated step 1 action.", "Updated step 1 result."),
                ])),
        };
        updateCaseRequest.Headers.Add("X-XSRF-TOKEN", await GetCsrfTokenAsync());
        var updateCaseResponse = await _client.SendAsync(updateCaseRequest);
        Assert.Equal(HttpStatusCode.OK, updateCaseResponse.StatusCode);
        var updatedCase = await updateCaseResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(updatedCase);
        Assert.Equal("Sign in with valid credentials - Updated", updatedCase.Title);
        Assert.Equal(2, updatedCase.Version);
        Assert.Single(updatedCase.Steps);
    }

    private async Task<HttpResponseMessage> CreateWorkspaceAsync(string name, string? prefix)
    {
        return await PostAsync(
            "/api/v1/test-workspaces",
            JsonContent.Create(new CreateTestWorkspaceRequest(name, prefix, null)),
            await GetCsrfTokenAsync());
    }

    private async Task<string> GetCsrfTokenAsync()
    {
        var response = await _client.GetAsync("/api/v1/auth/csrf-token");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        Assert.NotNull(payload);
        return payload.Token;
    }

    private Task<HttpResponseMessage> PostAsync(
        string path,
        HttpContent? content,
        string csrfToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content,
        };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return _client.SendAsync(request);
    }
}
