using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IssueTestTraceabilityEndpointsTests(ApiIntegrationTestFactory factory)
    : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

    [Fact]
    public async Task TraceabilityFlow_LinksRequirementsAndSnapshotsPlanTestIssueIntoRun()
    {
        var csrfToken = await GetCsrfTokenAsync();
        var initializeResponse = await PostAsync("/api/v1/setup/initialize", null, csrfToken);
        initializeResponse.EnsureSuccessStatusCode();
        var credentials = await initializeResponse.Content.ReadFromJsonAsync<InitializeAdminResponse>();
        Assert.NotNull(credentials);

        var loginResponse = await PostAsync(
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest
            {
                Username = "admin",
                Password = credentials.InitialPassword,
                RememberMe = false,
            }),
            csrfToken);
        loginResponse.EnsureSuccessStatusCode();

        var projectResponse = await PostAsync(
            "/api/v1/projects",
            JsonContent.Create(new CreateProjectRequest(
                code: "TRACE",
                name: "Traceability")
            {
                Description = null,
            }),
            await GetCsrfTokenAsync());
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);

        var requirement = await CreateIssueAsync(project.Id, "Checkout requirement", "story");
        var testTask = await CreateIssueAsync(project.Id, "Checkout verification", "task");
        var nextTestTask = await CreateIssueAsync(project.Id, "Checkout regression", "task");

        var workspaceResponse = await PostAsync(
            "/api/v1/test-workspaces",
            JsonContent.Create(new CreateTestWorkspaceRequest("Checkout QA")
            {
                Prefix = "CQA",
                Description = null,
            }),
            await GetCsrfTokenAsync());
        workspaceResponse.EnsureSuccessStatusCode();
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<TestWorkspaceResponse>();
        Assert.NotNull(workspace);

        var projectLinkResponse = await PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/projects",
            JsonContent.Create(new LinkTestWorkspaceProjectRequest(project.Id)),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, projectLinkResponse.StatusCode);
        var projectLink = await projectLinkResponse.Content
            .ReadFromJsonAsync<TestWorkspaceProjectResponse>();
        Assert.NotNull(projectLink);

        var suiteResponse = await PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/suites",
            JsonContent.Create(new CreateTestSuiteRequest
            {
                ParentId = null,
                Name = "Checkout",
                Description = null,
                SortOrder = 1,
            }),
            await GetCsrfTokenAsync());
        suiteResponse.EnsureSuccessStatusCode();
        var suite = await suiteResponse.Content.ReadFromJsonAsync<TestSuiteResponse>();
        Assert.NotNull(suite);

        var caseResponse = await PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            JsonContent.Create(new CreateTestCaseRequest(
                suiteId: suite.Id,
                title: "Submit checkout",
                steps:
                [
                    new CreateTestCaseStepRequest(
                        action: "Submit the checkout form.",
                        expectedResult: "The order is created."),
                ])
            {
                SortOrder = 1,
            }),
            await GetCsrfTokenAsync());
        caseResponse.EnsureSuccessStatusCode();
        var testCase = await caseResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(testCase);

        var requirementLinkResponse = await PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}/requirement-issues",
            JsonContent.Create(new LinkTestCaseRequirementIssueRequest(requirement.Id)),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, requirementLinkResponse.StatusCode);
        var requirementLink = await requirementLinkResponse.Content
            .ReadFromJsonAsync<TestCaseRequirementLinkResponse>();
        Assert.NotNull(requirementLink);
        Assert.Equal(requirement.Id, requirementLink.Issue.Id);
        Assert.Equal(requirement.Key, requirementLink.Issue.Key);

        var duplicateLinkResponse = await PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}/requirement-issues",
            JsonContent.Create(new LinkTestCaseRequirementIssueRequest(requirement.Id)),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Conflict, duplicateLinkResponse.StatusCode);

        var requirementLinks = await _client.GetFromJsonAsync<TestCaseRequirementLinkResponse[]>(
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}/requirement-issues");
        Assert.NotNull(requirementLinks);
        Assert.Equal(requirementLink.Id, Assert.Single(requirementLinks).Id);

        var invalidPlanResponse = await PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/plans",
            JsonContent.Create(new CreateTestPlanRequest(
                description: null,
                caseIds: [testCase.Id])
            {
                Name = "Invalid plan",
                TestIssueId = requirement.Id,
            }),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.BadRequest, invalidPlanResponse.StatusCode);

        var planResponse = await PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/plans",
            JsonContent.Create(new CreateTestPlanRequest(
                description: null,
                caseIds: [testCase.Id])
            {
                Name = "Checkout plan",
                TestIssueId = testTask.Id,
            }),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, planResponse.StatusCode);
        var draftPlan = await planResponse.Content.ReadFromJsonAsync<TestPlanResponse>();
        Assert.NotNull(draftPlan);
        Assert.Equal(testTask.Id, draftPlan.TestIssue?.Id);

        var activateResponse = await PutAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/plans/{draftPlan.Id}",
            new UpdateTestPlanRequest
            {
                Name = draftPlan.Name,
                Description = draftPlan.Description,
                Status = "active",
                Version = draftPlan.Version,
                CaseIds = [testCase.Id],
                TestIssueId = testTask.Id,
            });
        activateResponse.EnsureSuccessStatusCode();
        var activePlan = await activateResponse.Content.ReadFromJsonAsync<TestPlanResponse>();
        Assert.NotNull(activePlan);

        var runResponse = await PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs",
            JsonContent.Create(new CreateTestRunRequest(activePlan.Id, "Checkout run")),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, runResponse.StatusCode);
        var run = await runResponse.Content.ReadFromJsonAsync<TestRunResponse>();
        Assert.NotNull(run);
        Assert.Equal(testTask.Id, run.TestIssue?.Id);

        var changePlanResponse = await PutAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/plans/{activePlan.Id}",
            new UpdateTestPlanRequest
            {
                Name = activePlan.Name,
                Description = activePlan.Description,
                Status = "active",
                Version = activePlan.Version,
                CaseIds = [testCase.Id],
                TestIssueId = nextTestTask.Id,
            });
        changePlanResponse.EnsureSuccessStatusCode();
        var changedPlan = await changePlanResponse.Content.ReadFromJsonAsync<TestPlanResponse>();
        Assert.NotNull(changedPlan);
        Assert.Equal(nextTestTask.Id, changedPlan.TestIssue?.Id);

        var fetchedRun = await _client.GetFromJsonAsync<TestRunResponse>(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}");
        Assert.NotNull(fetchedRun);
        Assert.Equal(testTask.Id, fetchedRun.TestIssue?.Id);

        var createBugResponse = await PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/bugs",
            JsonContent.Create(new CreateTestRunBugRequest
            {
                ProjectId = project.Id,
                Title = "Checkout total is incorrect",
                PriorityCode = "high",
                Description = "The total differs from the confirmed cart.",
                AssigneeAccountId = null,
            }),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, createBugResponse.StatusCode);
        var bugLink = await createBugResponse.Content.ReadFromJsonAsync<TestRunBugLinkResponse>();
        Assert.NotNull(bugLink);
        Assert.Equal(run.Id, bugLink.TestRunId);
        Assert.Equal(project.Id, bugLink.Issue.ProjectId);
        Assert.Equal("bug", bugLink.Issue.TypeCode);
        Assert.Equal("Checkout total is incorrect", bugLink.Issue.Title);

        var createdBug = await _client.GetFromJsonAsync<IssueResponse>(
            $"/api/v1/projects/{project.Id}/issues/{bugLink.Issue.Id}");
        Assert.NotNull(createdBug);
        Assert.Equal("Checkout total is incorrect", createdBug.Title);
        Assert.Equal("The total differs from the confirmed cart.", createdBug.Description);
        Assert.Equal("high", createdBug.PriorityCode);

        var bugLinks = await _client.GetFromJsonAsync<TestRunBugLinkResponse[]>(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/bugs");
        Assert.NotNull(bugLinks);
        Assert.Equal(bugLink.Id, Assert.Single(bugLinks).Id);

        var unlinkProjectResponse = await DeleteAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/projects/{project.Id}?version={projectLink.Version}",
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Conflict, unlinkProjectResponse.StatusCode);

        var staleDeleteResponse = await DeleteAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}/requirement-issues/{requirementLink.Id}?version={requirementLink.Version + 1}",
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Conflict, staleDeleteResponse.StatusCode);

        var deleteResponse = await DeleteAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}/requirement-issues/{requirementLink.Id}?version={requirementLink.Version}",
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    private async Task<IssueResponse> CreateIssueAsync(Guid projectId, string title, string typeCode)
    {
        var response = await PostAsync(
            $"/api/v1/projects/{projectId}/issues",
            JsonContent.Create(new CreateIssueRequest(
                title: title,
                typeCode: typeCode)),
            await GetCsrfTokenAsync());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!;
    }

    private async Task<string> GetCsrfTokenAsync()
    {
        var response = await _client.GetFromJsonAsync<CsrfTokenResponse>("/api/v1/auth/csrf-token");
        Assert.NotNull(response);
        return response.Token;
    }

    private async Task<HttpResponseMessage> PostAsync(
        string path,
        HttpContent? content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAsync(string path, object content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(content),
        };
        request.Headers.Add("X-XSRF-TOKEN", await GetCsrfTokenAsync());
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAsync(string path, string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await _client.SendAsync(request);
    }
}
