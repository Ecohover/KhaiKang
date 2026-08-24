using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class ImmutableTraceabilitySourceTests
{
    [Fact]
    public async Task CaseSource_IsSingleImmutableAndIdempotent()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CASESRC");
        var firstIssue = await ApiTestData.CreateIssueAsync(api, project.Id, "Test checkout");
        var secondIssue = await ApiTestData.CreateIssueAsync(api, project.Id, "Test refund");
        var story = await ApiTestData.CreateIssueAsync(api, project.Id, "Checkout story", "story");
        var workspace = await ApiTestData.CreateWorkspaceAsync(api, "CSRC");
        await ApiTestData.LinkProjectAsync(api, workspace.Id, project.Id);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        var path = $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}/source-test-issue";

        var rejected = await PostIdempotentAsync(api, path, new LinkTestCaseSourceRequest(story.Id), "case-source-rejected");
        var rejectedReplay = await PostIdempotentAsync(api, path, new LinkTestCaseSourceRequest(story.Id), "case-source-rejected");
        var rejectedSubstitution = await PostIdempotentAsync(api, path, new LinkTestCaseSourceRequest(firstIssue.Id), "case-source-rejected");
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedReplay.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, rejectedSubstitution.StatusCode);

        var first = await PostIdempotentAsync(api, path, new LinkTestCaseSourceRequest(firstIssue.Id), "case-source-1");
        var replay = await PostIdempotentAsync(api, path, new LinkTestCaseSourceRequest(firstIssue.Id), "case-source-1");
        var substituted = await PostIdempotentAsync(api, path, new LinkTestCaseSourceRequest(secondIssue.Id), "case-source-1");
        var replacement = await PostIdempotentAsync(api, path, new LinkTestCaseSourceRequest(secondIssue.Id), "case-source-2");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<TestCaseSourceResponse>();
        var replayBody = await replay.Content.ReadFromJsonAsync<TestCaseSourceResponse>();
        Assert.Equal(firstBody?.Id, replayBody?.Id);
        Assert.Equal(HttpStatusCode.Conflict, substituted.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, replacement.StatusCode);
        var fetched = await api.Client.GetFromJsonAsync<TestCaseSourceResponse>(path);
        Assert.Equal(firstIssue.Id, fetched?.TestIssue.Id);
        var delete = await api.DeleteAsync(path);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, delete.StatusCode);
    }

    [Fact]
    public async Task BugSources_EnforceCardinalityAndRunItemMembership()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "BUGSRC");
        var testIssue = await ApiTestData.CreateIssueAsync(api, project.Id, "Test checkout");
        var bug = await ApiTestData.CreateIssueAsync(api, project.Id, "Checkout fails", "bug");
        var workspace = await ApiTestData.CreateWorkspaceAsync(api, "BSRC");
        await ApiTestData.LinkProjectAsync(api, workspace.Id, project.Id);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        var plan = await ApiTestData.CreatePlanAsync(api, workspace.Id, testCase.Id, true, testIssue.Id);
        var run = await ApiTestData.CreateRunAsync(api, workspace.Id, plan.Id);
        var item = Assert.Single(run.Items);
        var path = $"/api/v1/test-workspaces/{workspace.Id}/bugs/{bug.Id}/sources";
        var request = new LinkBugSourcesRequest { TestIssueId = testIssue.Id, TestRunId = run.Id, TestRunItemId = item.Id };

        var created = await PostIdempotentAsync(api, path, request, "bug-source-1");
        var replay = await PostIdempotentAsync(api, path, request, "bug-source-1");
        var duplicate = await PostIdempotentAsync(api, path, request, "bug-source-2");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var fetched = await api.Client.GetFromJsonAsync<BugSourcesResponse>(path);
        Assert.Equal(testIssue.Id, fetched?.TestIssue?.Id);
        Assert.Equal(run.Id, fetched?.TestRunId);
        Assert.Equal(item.Id, fetched?.TestRunItemId);
        var delete = await api.DeleteAsync(path);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, delete.StatusCode);

        var secondBug = await ApiTestData.CreateIssueAsync(api, project.Id, "Refund fails", "bug");
        var wrongItem = await PostIdempotentAsync(
            api,
            $"/api/v1/test-workspaces/{workspace.Id}/bugs/{secondBug.Id}/sources",
            new LinkBugSourcesRequest { TestRunId = run.Id, TestRunItemId = Guid.NewGuid() },
            "bug-source-wrong-item");
        Assert.Equal(HttpStatusCode.BadRequest, wrongItem.StatusCode);

        var legacyCreate = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/runs/{run.Id}/bugs",
            new CreateTestRunBugRequest
            {
                ProjectId = project.Id,
                Title = "Legacy run bug",
                PriorityCode = "high",
                Description = null,
                AssigneeAccountId = null,
            });
        Assert.Equal(HttpStatusCode.Created, legacyCreate.StatusCode);
        var legacyBug = await legacyCreate.Content.ReadFromJsonAsync<TestRunBugLinkResponse>();
        Assert.NotNull(legacyBug);
        var enrichLegacy = await PostIdempotentAsync(
            api,
            $"/api/v1/test-workspaces/{workspace.Id}/bugs/{legacyBug.Issue.Id}/sources",
            request,
            "bug-source-existing-run");
        Assert.Equal(HttpStatusCode.Created, enrichLegacy.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostIdempotentAsync(AuthenticatedApiTestContext api, string path, object body, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-XSRF-TOKEN", await AuthenticatedApiTestContext.GetCsrfTokenAsync(api.Client));
        request.Headers.Add("Idempotency-Key", key);
        return await api.Client.SendAsync(request);
    }
}
