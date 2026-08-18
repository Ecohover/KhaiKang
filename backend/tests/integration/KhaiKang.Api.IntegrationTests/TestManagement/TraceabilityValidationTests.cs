using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TraceabilityValidationTests
{
    [Fact]
    public async Task LinkRequirement_RequiresLinkedActiveProject()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);
        var requirement = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Checkout requirement",
            "story");
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        var path = $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}/requirement-issues";

        var unlinkedProject = await api.PostJsonAsync(
            path,
            new LinkTestCaseRequirementIssueRequest(requirement.Id));
        await AssertValidationCodeAsync(
            unlinkedProject,
            "workspace_project_not_linked");

        await ApiTestData.LinkProjectAsync(api, workspace.Id, project.Id);
        var deactivateResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}",
            new UpdateProjectRequest(
                name: project.Name,
                status: "inactive",
                version: project.Version)
            {
                Description = project.Description,
            });
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var inactiveProject = await api.PostJsonAsync(
            path,
            new LinkTestCaseRequirementIssueRequest(requirement.Id));
        await AssertConflictCodeAsync(inactiveProject, "project_not_active");
    }

    [Fact]
    public async Task PlanAndRunBug_RequireLinkedActiveProjectAndValidIssueOptions()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);
        var testTask = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Checkout verification");
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);

        var unlinkedPlan = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/plans",
            new CreateTestPlanRequest(
                description: null,
                caseIds: [testCase.Id])
            {
                Name = "Unlinked plan",
                TestIssueId = testTask.Id,
            });
        await AssertValidationCodeAsync(
            unlinkedPlan,
            "workspace_project_not_linked");

        await ApiTestData.LinkProjectAsync(api, workspace.Id, project.Id);
        var plan = await ApiTestData.CreatePlanAsync(
            api,
            workspace.Id,
            testCase.Id,
            activate: true,
            testTask.Id);
        var run = await ApiTestData.CreateRunAsync(
            api,
            workspace.Id,
            plan.Id);

        var invalidPriority = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/bugs",
            new CreateTestRunBugRequest
            {
                ProjectId = project.Id,
                Title = "Checkout is incorrect",
                PriorityCode = "urgent",
                Description = null,
                AssigneeAccountId = null,
            });
        await AssertValidationCodeAsync(
            invalidPriority,
            "bug_issue_option_invalid");

        var deactivateResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}",
            new UpdateProjectRequest(
                name: project.Name,
                status: "inactive",
                version: project.Version)
            {
                Description = project.Description,
            });
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var inactiveProject = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/bugs",
            new CreateTestRunBugRequest
            {
                ProjectId = project.Id,
                Title = "Checkout is incorrect",
                PriorityCode = "high",
                Description = null,
                AssigneeAccountId = null,
            });
        await AssertConflictCodeAsync(inactiveProject, "project_not_active");
    }

    private static async Task AssertValidationCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var root = problem.RootElement;
        if (root.TryGetProperty("code", out var code))
        {
            Assert.Equal(expectedCode, code.GetString());
            return;
        }

        Assert.True(root.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("request", out var requestErrors));
        Assert.Equal(expectedCode, requestErrors[0].GetString());
    }

    private static async Task AssertConflictCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
        Assert.NotNull(problem);
        Assert.Equal(expectedCode, problem.Code);
    }

    private sealed record ApiProblem(string? Code);
}
