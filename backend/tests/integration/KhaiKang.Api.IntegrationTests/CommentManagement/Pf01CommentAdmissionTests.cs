using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class Pf01CommentAdmissionTests
{
    [Fact]
    public async Task ReviewerAi_CreatesThenListsComment_UsingOwnerDerivedProjectScope()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01REV");
        var task = await ApiTestData.CreateIssueAsync(api, project.Id, "Review target");
        var development = await CreateAiBearerAsync(api, "ai-pf01-development", "development_ai");
        var reviewer = await CreateAiBearerAsync(api, "ai-pf01-reviewer", "reviewer_ai");
        using var assignResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/issues/{task.Id}/assignee",
            new { assigneeAccountId = development.AccountId, version = task.Version });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var path = $"/api/v1/comments/issue/{task.Id}";
        using var reviewerClient = BearerClient(api, reviewer.Token);

        using var createResponse = await PostCommentAsync(
            reviewerClient,
            path,
            "Review evidence.");
        using var listResponse = await reviewerClient.GetAsync(path);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CommentResponse>();
        var page = await listResponse.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(created);
        Assert.NotNull(page);
        Assert.True(page.CanCreate);
        Assert.Equal(created.Id, Assert.Single(page.Items).Id);
        Assert.Equal(reviewer.AccountId, created.AuthorAccountId);
    }

    [Fact]
    public async Task ReviewerAi_OnReadableUserStory_CannotCreateComment()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01DENY");
        var story = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Readable requirement",
            "story");
        var reviewer = await CreateAiBearerAsync(api, "ai-pf01-review-denied", "reviewer_ai");
        var path = $"/api/v1/comments/issue/{story.Id}";
        using var reviewerClient = BearerClient(api, reviewer.Token);

        using var listResponse = await reviewerClient.GetAsync(path);
        using var createResponse = await PostCommentAsync(
            reviewerClient,
            path,
            "Must not be created.");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(page);
        Assert.False(page.CanCreate);
        Assert.Empty(page.Items);
        Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);
    }

    [Fact]
    public async Task QaAi_TestAssetComments_RequireAssignedTestingContext()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01TEST");
        var story = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Requirement under test",
            "story");
        var testIssue = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Assigned test issue");
        var qa = await CreateAiBearerAsync(api, "ai-pf01-test-assets", "qa_ai");
        await AssignIssueAsync(api, project.Id, testIssue, qa.AccountId);
        using var relationResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/issues/{testIssue.Id}/relations",
            new CreateIssueRelationRequest("tests", story.Id, "forward"));
        Assert.Equal(HttpStatusCode.Created, relationResponse.StatusCode);
        var workspace = await ApiTestData.CreateWorkspaceAsync(api, "PF01TEST");
        await ApiTestData.LinkProjectAsync(api, workspace.Id, project.Id);
        var sourcedCase = await ApiTestData.CreateCaseAsync(api, workspace.Id, "Sourced case");
        using var sourceResponse = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{sourcedCase.Id}/source-test-issue",
            new LinkTestCaseSourceRequest(testIssue.Id));
        Assert.Equal(HttpStatusCode.Created, sourceResponse.StatusCode);
        var plan = await ApiTestData.CreatePlanAsync(
            api,
            workspace.Id,
            sourcedCase.Id,
            activate: true,
            testIssueId: testIssue.Id);
        var run = await ApiTestData.CreateRunAsync(api, workspace.Id, plan.Id);
        var unsourcedWorkspace = await ApiTestData.CreateWorkspaceAsync(api, "PF01MISS");
        var unsourcedCase = await ApiTestData.CreateCaseAsync(
            api,
            unsourcedWorkspace.Id,
            "Readable case without testing context");
        using var qaClient = BearerClient(api, qa.Token);
        var allowedResources = new[]
        {
            new CommentTarget("test_case", sourcedCase.Id),
            new CommentTarget("test_plan", plan.Id),
            new CommentTarget("test_run", run.Id),
        };

        foreach (var resource in allowedResources)
        {
            using var createResponse = await PostCommentAsync(
                qaClient,
                resource.Path,
                $"Evidence for {resource.Type}.");
            using var listResponse = await qaClient.GetAsync(resource.Path);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var page = await listResponse.Content.ReadFromJsonAsync<CommentPageResponse>();
            Assert.NotNull(page);
            Assert.True(page.CanCreate);
            Assert.Single(page.Items);
        }

        var unsourcedPath = $"/api/v1/comments/test_case/{unsourcedCase.Id}";
        using var unsourcedListResponse = await qaClient.GetAsync(unsourcedPath);
        using var unsourcedCreateResponse = await PostCommentAsync(
            qaClient,
            unsourcedPath,
            "Context is required.");

        Assert.Equal(HttpStatusCode.OK, unsourcedListResponse.StatusCode);
        var unsourcedPage = await unsourcedListResponse.Content
            .ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(unsourcedPage);
        Assert.False(unsourcedPage.CanCreate);
        Assert.Empty(unsourcedPage.Items);
        Assert.Equal(HttpStatusCode.NotFound, unsourcedCreateResponse.StatusCode);
    }

    [Fact]
    public async Task AiRoles_CreateAndListComments_OnlyOnTheirAllowedProjectTargets()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01ROLE");
        var story = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Handoff story",
            "story");
        var developmentTask = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Development task");
        var testIssue = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Test issue");
        var devOpsTask = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Diagnostic task");
        var pm = await CreateAiBearerAsync(api, "ai-pf01-pm", "pm_ai");
        var development = await CreateAiBearerAsync(
            api,
            "ai-pf01-development-matrix",
            "development_ai");
        var qa = await CreateAiBearerAsync(api, "ai-pf01-qa-matrix", "qa_ai");
        var reviewer = await CreateAiBearerAsync(
            api,
            "ai-pf01-reviewer-matrix",
            "reviewer_ai");
        var devOps = await CreateAiBearerAsync(api, "ai-pf01-devops", "devops_ai");
        await AssignIssueAsync(api, project.Id, developmentTask, development.AccountId);
        await AssignIssueAsync(api, project.Id, testIssue, qa.AccountId);
        using var relationResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/issues/{testIssue.Id}/relations",
            new CreateIssueRelationRequest("tests", story.Id, "forward"));
        Assert.Equal(HttpStatusCode.Created, relationResponse.StatusCode);
        var scenarios = new[]
        {
            new ProjectCommentScenario(pm, story, "pm_ai"),
            new ProjectCommentScenario(development, developmentTask, "development_ai"),
            new ProjectCommentScenario(qa, testIssue, "qa_ai"),
            new ProjectCommentScenario(reviewer, developmentTask, "reviewer_ai"),
            new ProjectCommentScenario(devOps, devOpsTask, "devops_ai"),
        };

        foreach (var scenario in scenarios)
        {
            var path = $"/api/v1/comments/issue/{scenario.Issue.Id}";
            using var client = BearerClient(api, scenario.Actor.Token);
            using var createResponse = await PostCommentAsync(
                client,
                path,
                $"{scenario.FixedRole} evidence.");
            using var listResponse = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var created = await createResponse.Content.ReadFromJsonAsync<CommentResponse>();
            var page = await listResponse.Content.ReadFromJsonAsync<CommentPageResponse>();
            Assert.NotNull(created);
            Assert.NotNull(page);
            Assert.Equal(scenario.FixedRole, created.FixedRole);
            Assert.True(page.CanCreate);
            Assert.Contains(page.Items, item => item.Id == created.Id);
        }
    }

    [Fact]
    public async Task DevelopmentAi_MissingAssignment_DoesNotReserveIdempotencyKey()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01IDEM");
        var task = await ApiTestData.CreateIssueAsync(api, project.Id, "Assignment required");
        var development = await CreateAiBearerAsync(
            api,
            "ai-pf01-development-idempotency",
            "development_ai");
        using var client = BearerClient(api, development.Token);
        var path = $"/api/v1/comments/issue/{task.Id}";
        var idempotencyKey = Guid.NewGuid().ToString("N");
        const string content = "Implementation evidence.";

        using var deniedResponse = await PostCommentAsync(
            client,
            path,
            content,
            idempotencyKey);
        using var deniedListResponse = await client.GetAsync(path);
        await AssignIssueAsync(api, project.Id, task, development.AccountId);
        using var acceptedResponse = await PostCommentAsync(
            client,
            path,
            content,
            idempotencyKey);

        Assert.Equal(HttpStatusCode.NotFound, deniedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, deniedListResponse.StatusCode);
        var deniedPage = await deniedListResponse.Content
            .ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(deniedPage);
        Assert.False(deniedPage.CanCreate);
        Assert.Empty(deniedPage.Items);
        Assert.Equal(HttpStatusCode.Created, acceptedResponse.StatusCode);
    }

    [Fact]
    public async Task HumanCommentAccess_PreservesExistingCreateAndCsrfBehavior()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01HUMDEV");
        var task = await ApiTestData.CreateIssueAsync(api, project.Id, "Human target");
        var path = $"/api/v1/comments/issue/{task.Id}";

        using var listResponse = await api.Client.GetAsync(path);
        using var validCreateResponse = await api.PostJsonAsync(
            path,
            new { content = "Human comment." });
        using var missingCsrfResponse = await PostCommentAsync(
            api.Client,
            path,
            "Missing CSRF must fail.");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(page);
        Assert.True(page.CanCreate);
        Assert.Equal(HttpStatusCode.Created, validCreateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrfResponse.StatusCode);
    }

    private static async Task<AiBearer> CreateAiBearerAsync(
        AuthenticatedApiTestContext api,
        string username,
        string fixedRole)
    {
        using var createResponse = await api.PostJsonAsync(
            "/api/v1/ai-accounts",
            new { username, fixedRole });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var account = (await ReadJsonAsync(createResponse)).GetProperty("account");
        var accountId = account.GetProperty("id").GetGuid();
        using var enableResponse = await api.PutJsonAsync(
            $"/api/v1/ai-accounts/{accountId}/api-enabled",
            new { enabled = true, version = account.GetProperty("version").GetInt32() });
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        using var tokenResponse = await api.PostJsonAsync(
            $"/api/v1/ai-accounts/{accountId}/token",
            new { });
        Assert.Equal(HttpStatusCode.Created, tokenResponse.StatusCode);
        return new AiBearer(
            accountId,
            (await ReadJsonAsync(tokenResponse)).GetProperty("token").GetString()!);
    }

    private static HttpClient BearerClient(
        AuthenticatedApiTestContext api,
        string token)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task AssignIssueAsync(
        AuthenticatedApiTestContext api,
        Guid projectId,
        IssueResponse issue,
        Guid assigneeAccountId)
    {
        using var response = await api.PutJsonAsync(
            $"/api/v1/projects/{projectId}/issues/{issue.Id}/assignee",
            new { assigneeAccountId, version = issue.Version });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostCommentAsync(
        HttpClient client,
        string path,
        string content,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { content }),
        };
        request.Headers.Add(
            "Idempotency-Key",
            idempotencyKey ?? Guid.NewGuid().ToString("N"));
        return await client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        return content;
    }

    private sealed record AiBearer(Guid AccountId, string Token);

    private sealed record ProjectCommentScenario(
        AiBearer Actor,
        IssueResponse Issue,
        string FixedRole);

    private sealed record CommentTarget(string Type, Guid Id)
    {
        public string Path => $"/api/v1/comments/{Type}/{Id}";
    }
}
