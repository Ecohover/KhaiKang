using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TestPlanRunBehaviorTests
{
    [Fact]
    public async Task UpdatePlan_WithStaleVersionOrEmptyActiveScope_RejectsMutation()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        var plan = await ApiTestData.CreatePlanAsync(
            api,
            workspace.Id,
            testCase.Id,
            activate: false);
        var path = $"/api/v1/test-workspaces/{workspace.Id}/plans/{plan.Id}";

        var staleUpdate = await api.PutJsonAsync(
            path,
            new UpdateTestPlanRequest
            {
                Name = plan.Name,
                Description = plan.Description,
                Status = "active",
                Version = plan.Version + 1,
                CaseIds = [testCase.Id],
            });
        await AssertConflictCodeAsync(staleUpdate, "plan_version_conflict");

        var emptyActivePlan = await api.PutJsonAsync(
            path,
            new UpdateTestPlanRequest
            {
                Name = plan.Name,
                Description = plan.Description,
                Status = "active",
                Version = plan.Version,
                CaseIds = [],
            });
        Assert.Equal(HttpStatusCode.BadRequest, emptyActivePlan.StatusCode);
    }

    [Fact]
    public async Task CreateRun_FromDraftPlanOrPlanWithInactiveCase_ReturnsConflict()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        var draftPlan = await ApiTestData.CreatePlanAsync(
            api,
            workspace.Id,
            testCase.Id,
            activate: false);

        var draftRun = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs",
            new CreateTestRunRequest(draftPlan.Id, "Draft plan run"));
        await AssertConflictCodeAsync(draftRun, "plan_not_active");

        var activateResponse = await api.PutJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/plans/{draftPlan.Id}",
            new UpdateTestPlanRequest
            {
                Name = draftPlan.Name,
                Description = draftPlan.Description,
                Status = "active",
                Version = draftPlan.Version,
                CaseIds = [testCase.Id],
            });
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        var deactivateCaseResponse = await api.PutJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}",
            new UpdateTestCaseRequest(
                suiteId: testCase.SuiteId,
                title: testCase.Title,
                steps: testCase.Steps.Select(step =>
                    new CreateTestCaseStepRequest(
                        action: step.Action,
                        expectedResult: step.ExpectedResult)).ToArray())
            {
                Description = testCase.Description,
                Preconditions = testCase.Preconditions,
                OverallExpectedResult = testCase.OverallExpectedResult,
                SortOrder = testCase.SortOrder,
                Status = "inactive",
                Version = testCase.Version,
            });
        Assert.Equal(HttpStatusCode.OK, deactivateCaseResponse.StatusCode);

        var inactiveCaseRun = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs",
            new CreateTestRunRequest(draftPlan.Id, "Inactive case run"));
        await AssertConflictCodeAsync(
            inactiveCaseRun,
            "plan_contains_inactive_case");
    }

    [Fact]
    public async Task Run_EnforcesVersionsCompletionAndTerminalRerunRules()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        var plan = await ApiTestData.CreatePlanAsync(
            api,
            workspace.Id,
            testCase.Id,
            activate: true);
        var run = await ApiTestData.CreateRunAsync(api, workspace.Id, plan.Id);
        var item = Assert.Single(run.Items);
        var step = Assert.Single(item.Steps);

        var prematureRerun = await api.PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/rerun");
        await AssertConflictCodeAsync(prematureRerun, "run_not_terminal");

        var staleRunStatus = await api.PutJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/status",
            new UpdateTestRunStatusRequest(
                "completed",
                "Must not be accepted.",
                run.Version + 1));
        await AssertConflictCodeAsync(staleRunStatus, "run_version_conflict");

        var unfinishedCompletion = await api.PutJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/status",
            new UpdateTestRunStatusRequest(
                "completed",
                "Must not be accepted.",
                run.Version));
        await AssertConflictCodeAsync(
            unfinishedCompletion,
            "run_has_unfinished_items");

        var staleItemResult = await api.PutJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/items/{item.Id}",
            new RecordTestResultRequest(
                "passed",
                "Must not be accepted.",
                item.Version + 1));
        await AssertConflictCodeAsync(
            staleItemResult,
            "run_item_version_conflict");

        var staleStepResult = await api.PutJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/items/{item.Id}/steps/{step.Id}",
            new RecordTestResultRequest(
                "passed",
                "Must not be accepted.",
                step.Version + 1));
        await AssertConflictCodeAsync(
            staleStepResult,
            "run_step_version_conflict");

        var cancelResponse = await api.PutJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/status",
            new UpdateTestRunStatusRequest(
                "cancelled",
                "Cancelled for rerun verification.",
                run.Version));
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelledRun = Assert.IsType<TestRunResponse>(
            await cancelResponse.Content.ReadFromJsonAsync<TestRunResponse>());

        var rerunResponse = await api.PostAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/rerun");
        Assert.Equal(HttpStatusCode.Created, rerunResponse.StatusCode);
        var rerun = Assert.IsType<TestRunResponse>(
            await rerunResponse.Content.ReadFromJsonAsync<TestRunResponse>());
        Assert.Equal(cancelledRun.RunNo + 1, rerun.RunNo);
        Assert.Equal($"{cancelledRun.Name} rerun", rerun.Name);
        Assert.Equal("not_started", rerun.Status);
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
