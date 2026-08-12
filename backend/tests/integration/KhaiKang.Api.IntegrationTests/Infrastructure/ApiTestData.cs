using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

internal static class ApiTestData
{
    public static async Task<ProjectResponse> CreateProjectAsync(
        AuthenticatedApiTestContext api,
        string code = "REF")
    {
        var response = await api.PostJsonAsync(
            "/api/v1/projects",
            new CreateProjectRequest(code, $"{code} Project", null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<ProjectResponse>(
            await response.Content.ReadFromJsonAsync<ProjectResponse>());
    }

    public static async Task<IssueResponse> CreateIssueAsync(
        AuthenticatedApiTestContext api,
        Guid projectId,
        string title,
        string typeCode = "task")
    {
        var response = await api.PostJsonAsync(
            $"/api/v1/projects/{projectId}/issues",
            new CreateIssueRequest(
                title: title,
                typeCode: typeCode));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<IssueResponse>(
            await response.Content.ReadFromJsonAsync<IssueResponse>());
    }

    public static async Task<TestWorkspaceResponse> CreateWorkspaceAsync(
        AuthenticatedApiTestContext api,
        string prefix = "REF")
    {
        var response = await api.PostJsonAsync(
            "/api/v1/test-workspaces",
            new CreateTestWorkspaceRequest($"{prefix} Workspace", prefix, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<TestWorkspaceResponse>(
            await response.Content.ReadFromJsonAsync<TestWorkspaceResponse>());
    }

    public static async Task<TestWorkspaceProjectResponse> LinkProjectAsync(
        AuthenticatedApiTestContext api,
        Guid workspaceId,
        Guid projectId)
    {
        var response = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/projects",
            new LinkTestWorkspaceProjectRequest(projectId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<TestWorkspaceProjectResponse>(
            await response.Content.ReadFromJsonAsync<TestWorkspaceProjectResponse>());
    }

    public static async Task<TestCaseResponse> CreateCaseAsync(
        AuthenticatedApiTestContext api,
        Guid workspaceId,
        string title = "Checkout succeeds")
    {
        var suiteResponse = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/suites",
            new CreateTestSuiteRequest(null, "Checkout", null, 1));
        Assert.Equal(HttpStatusCode.Created, suiteResponse.StatusCode);
        var suite = Assert.IsType<TestSuiteResponse>(
            await suiteResponse.Content.ReadFromJsonAsync<TestSuiteResponse>());

        var caseResponse = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/cases",
            new CreateTestCaseRequest(
                suiteId: suite.Id,
                title: title,
                steps:
                [
                    new CreateTestCaseStepRequest(
                        action: "Perform the operation.",
                        expectedResult: "The result is visible."),
                ])
            {
                Description = "Case description.",
                Preconditions = "An active account exists.",
                OverallExpectedResult = "The operation succeeds.",
                SortOrder = 1,
            });
        Assert.Equal(HttpStatusCode.Created, caseResponse.StatusCode);
        return Assert.IsType<TestCaseResponse>(
            await caseResponse.Content.ReadFromJsonAsync<TestCaseResponse>());
    }

    public static async Task<TestPlanResponse> CreatePlanAsync(
        AuthenticatedApiTestContext api,
        Guid workspaceId,
        Guid caseId,
        bool activate,
        Guid? testIssueId = null)
    {
        var createResponse = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/plans",
            new CreateTestPlanRequest(
                "Regression plan",
                "Stable refactoring scope.",
                [caseId],
                testIssueId));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var plan = Assert.IsType<TestPlanResponse>(
            await createResponse.Content.ReadFromJsonAsync<TestPlanResponse>());
        if (!activate)
        {
            return plan;
        }

        var activateResponse = await api.PutJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/plans/{plan.Id}",
            new UpdateTestPlanRequest(
                plan.Name,
                plan.Description,
                "active",
                plan.Version,
                [caseId],
                testIssueId));
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
        return Assert.IsType<TestPlanResponse>(
            await activateResponse.Content.ReadFromJsonAsync<TestPlanResponse>());
    }

    public static async Task<TestRunResponse> CreateRunAsync(
        AuthenticatedApiTestContext api,
        Guid workspaceId,
        Guid planId,
        string name = "Regression run")
    {
        var response = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/runs",
            new CreateTestRunRequest(planId, name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<TestRunResponse>(
            await response.Content.ReadFromJsonAsync<TestRunResponse>());
    }
}
