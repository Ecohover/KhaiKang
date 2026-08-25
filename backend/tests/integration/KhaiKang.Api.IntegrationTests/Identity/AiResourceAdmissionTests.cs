using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using KhaiKang.Modules.TestManagement.Contracts;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KhaiKang.Api.IntegrationTests.Identity;

public sealed class AiResourceAdmissionTests
{
    [Fact]
    public async Task TestAssets_AllowQaAiDenyOtherRolesAndReevaluateOwnerWorkspaceScope()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api, "AIQA");
        var suiteResponse = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/suites",
            new CreateTestSuiteRequest
            {
                ParentId = null,
                Name = "AI execution",
                Description = null,
                SortOrder = 1,
            });
        suiteResponse.EnsureSuccessStatusCode();
        var suite = await suiteResponse.Content.ReadFromJsonAsync<TestSuiteResponse>();
        Assert.NotNull(suite);

        var qa = await CreateAiBearerAsync(api, "ai-qa-assets", "qa_ai");
        var pm = await CreateAiBearerAsync(api, "ai-pm-test-denied", "pm_ai");
        var reviewer = await CreateAiBearerAsync(api, "ai-reviewer-test-read", "reviewer_ai");
        using var qaClient = BearerClient(api, qa.Token);
        using var pmClient = BearerClient(api, pm.Token);
        using var reviewerClient = BearerClient(api, reviewer.Token);

        var visibleWorkspaces = await qaClient.GetFromJsonAsync<TestWorkspaceResponse[]>("/api/v1/test-workspaces");
        Assert.Contains(visibleWorkspaces!, item => item.Id == workspace.Id);
        var request = TestCaseRequest(suite.Id, "QA-authored case");

        using var qaCreate = await PostJsonAsync(
            qaClient,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            request);
        Assert.Equal(HttpStatusCode.Created, qaCreate.StatusCode);

        using var pmCreate = await PostJsonAsync(
            pmClient,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            TestCaseRequest(suite.Id, "PM cannot author test assets"));
        Assert.Equal(HttpStatusCode.Forbidden, pmCreate.StatusCode);

        using var reviewerRead = await reviewerClient.GetAsync($"/api/v1/test-workspaces/{workspace.Id}/cases");
        Assert.Equal(HttpStatusCode.OK, reviewerRead.StatusCode);

        var ownerResponse = await api.PostJsonAsync("/api/v1/accounts", new { username = "qa-scope-new-owner" });
        ownerResponse.EnsureSuccessStatusCode();
        var owner = await ReadJsonAsync(ownerResponse);
        var ownerAccount = owner.GetProperty("account");
        var transfer = await api.PutJsonAsync(
            $"/api/v1/ai-accounts/{qa.AccountId}/owner",
            new { ownerAccountId = ownerAccount.GetProperty("id").GetGuid(), version = qa.Version });
        transfer.EnsureSuccessStatusCode();

        using var newOwnerClient = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            newOwnerClient,
            ownerAccount.GetProperty("username").GetString()!,
            owner.GetProperty("initialPassword").GetString()!);
        var recreate = await AuthenticatedApiTestContext.PostJsonAsync(
            newOwnerClient,
            $"/api/v1/ai-accounts/{qa.AccountId}/token",
            new { });
        recreate.EnsureSuccessStatusCode();
        var replacementToken = (await ReadJsonAsync(recreate)).GetProperty("token").GetString()!;
        using var transferredQaClient = BearerClient(api, replacementToken);
        using var crossScopeRead = await transferredQaClient.GetAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/cases");
        Assert.Equal(HttpStatusCode.NotFound, crossScopeRead.StatusCode);

        using var scope = api.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestManagementDbContext>();
        var operation = await dbContext.OperationEvents.AsNoTracking()
            .SingleAsync(item => item.EventType == "test_case_created");
        Assert.Equal("ai_agent", operation.ActorType);
        Assert.Equal("ai-qa-assets", operation.ActorNameSnapshot);
        Assert.Equal("qa_ai", operation.ActorFixedRoleSnapshot);
        Assert.Equal("admin", operation.OwnerDisplayNameSnapshot);

        var visibleOperations = await api.Client.GetFromJsonAsync<ResourceOperationResponse[]>(
            $"/api/v1/test-workspaces/{workspace.Id}/operations/test_case/{operation.ResourceId}");
        var visibleOperation = Assert.Single(visibleOperations!);
        Assert.Equal("qa_ai", visibleOperation.FixedRole);
        Assert.Equal("admin", visibleOperation.OwnerDisplayName);
        Assert.DoesNotContain(qa.Token, JsonSerializer.Serialize(visibleOperations), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProjectMutations_IntersectFixedRoleWithOwnerScopeAndOwnAssignment()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "AIROLE");
        var pm = await CreateAiBearerAsync(api, "ai-pm-resource", "pm_ai");
        var development = await CreateAiBearerAsync(api, "ai-development-resource", "development_ai");
        var qa = await CreateAiBearerAsync(api, "ai-qa-bug-owner", "qa_ai");
        var reviewer = await CreateAiBearerAsync(api, "ai-reviewer-resource", "reviewer_ai");
        using var pmClient = BearerClient(api, pm.Token);
        using var developmentClient = BearerClient(api, development.Token);
        using var qaClient = BearerClient(api, qa.Token);
        using var reviewerClient = BearerClient(api, reviewer.Token);

        var visibleProjects = await pmClient.GetFromJsonAsync<ProjectResponse[]>("/api/v1/projects");
        Assert.Contains(visibleProjects!, item => item.Id == project.Id);

        using var pmCreate = await PostJsonAsync(
            pmClient,
            $"/api/v1/projects/{project.Id}/issues",
            new CreateIssueRequest("Implement admission", "task"));
        Assert.True(
            pmCreate.StatusCode == HttpStatusCode.Created,
            await pmCreate.Content.ReadAsStringAsync());
        var task = await pmCreate.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(task);

        using var pmBug = await PostJsonAsync(
            pmClient,
            $"/api/v1/projects/{project.Id}/issues",
            new CreateIssueRequest("Forbidden PM bug", "bug"));
        Assert.Equal(HttpStatusCode.Forbidden, pmBug.StatusCode);

        using var qaBug = await PostJsonAsync(
            qaClient,
            $"/api/v1/projects/{project.Id}/issues",
            new CreateIssueRequest("QA-owned defect", "bug"));
        Assert.Equal(HttpStatusCode.Created, qaBug.StatusCode);
        var bug = await qaBug.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Equal(qa.AccountId, bug!.AssigneeAccountId);

        using var reviewerCreate = await PostJsonAsync(
            reviewerClient,
            $"/api/v1/projects/{project.Id}/issues",
            new CreateIssueRequest("Reviewer cannot create", "story"));
        Assert.Equal(HttpStatusCode.Forbidden, reviewerCreate.StatusCode);

        using var unassignedUpdate = await PutJsonAsync(
            developmentClient,
            $"/api/v1/projects/{project.Id}/issues/{task.Id}",
            UpdateTask(task, "Unassigned update denied"));
        Assert.Equal(HttpStatusCode.Forbidden, unassignedUpdate.StatusCode);

        var assigned = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/issues/{task.Id}/assignee",
            new { assigneeAccountId = development.AccountId, version = task.Version });
        assigned.EnsureSuccessStatusCode();
        task = await assigned.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(task);
        using var requirementRewrite = await PutJsonAsync(
            developmentClient,
            $"/api/v1/projects/{project.Id}/issues/{task.Id}",
            UpdateTask(task, "Implementation completed", "Rewritten requirement"));
        Assert.Equal(HttpStatusCode.Forbidden, requirementRewrite.StatusCode);
        using var ownUpdate = await PutJsonAsync(
            developmentClient,
            $"/api/v1/projects/{project.Id}/issues/{task.Id}",
            UpdateTask(task, "Implementation completed"));
        Assert.Equal(HttpStatusCode.OK, ownUpdate.StatusCode);

        var ownerResponse = await api.PostJsonAsync("/api/v1/accounts", new { username = "project-snapshot-owner" });
        ownerResponse.EnsureSuccessStatusCode();
        var owner = await ReadJsonAsync(ownerResponse);
        var transfer = await api.PutJsonAsync(
            $"/api/v1/ai-accounts/{development.AccountId}/owner",
            new
            {
                ownerAccountId = owner.GetProperty("account").GetProperty("id").GetGuid(),
                version = development.Version,
            });
        transfer.EnsureSuccessStatusCode();

        using var scope = api.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProjectManagementDbContext>();
        var operation = await dbContext.ProjectAuditEvents.AsNoTracking()
            .SingleAsync(item => item.TargetId == task.Id && item.EventType == "issue_updated");
        Assert.Equal("ai_agent", operation.ActorType);
        Assert.Equal("ai-development-resource", operation.ActorNameSnapshot);
        Assert.Equal("development_ai", operation.ActorFixedRoleSnapshot);
        Assert.Equal("admin", operation.OwnerDisplayNameSnapshot);

        var visibleOperations = await api.Client.GetFromJsonAsync<ResourceOperationResponse[]>(
            $"/api/v1/projects/{project.Id}/issues/{task.Id}/operations");
        Assert.Contains(visibleOperations!, item =>
            item.EventType == "issue_updated" &&
            item.FixedRole == "development_ai" &&
            item.OwnerDisplayName == "admin");
        Assert.DoesNotContain(
            development.Token,
            JsonSerializer.Serialize(visibleOperations),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReviewerAi_CanCommentWithinOwnerScopeAndSnapshotSurvivesOwnerTransfer()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "AICOMMENT");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Review this task");
        var development = await CreateAiBearerAsync(
            api,
            "ai-development-review-target",
            "development_ai");
        var reviewer = await CreateAiBearerAsync(api, "ai-reviewer-snapshot", "reviewer_ai");
        var devops = await CreateAiBearerAsync(api, "ai-devops-diagnostics", "devops_ai");
        using var assign = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/issues/{issue.Id}/assignee",
            new { assigneeAccountId = development.AccountId, version = issue.Version });
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
        issue = Assert.IsType<IssueResponse>(await assign.Content.ReadFromJsonAsync<IssueResponse>());
        using var bearer = api.CreateClient();
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reviewer.Token);
        using var devopsBearer = BearerClient(api, devops.Token);

        using var create = await PostCommentAsync(bearer, issue.Id, "Review evidence.", "reviewer-comment");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var comment = await create.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(comment);
        Assert.Equal("ai_agent", comment.AccountType);
        Assert.Equal("reviewer_ai", comment.FixedRole);
        Assert.Equal("admin", comment.OwnerDisplayName);

        using var devopsComment = await PostCommentAsync(
            devopsBearer,
            issue.Id,
            "Deployment diagnostic evidence.",
            "devops-comment");
        Assert.Equal(HttpStatusCode.Created, devopsComment.StatusCode);
        using var devopsMutation = await PutJsonAsync(
            devopsBearer,
            $"/api/v1/projects/{project.Id}/issues/{issue.Id}",
            UpdateTask(issue, "DevOps must not mutate task"));
        Assert.Equal(HttpStatusCode.Forbidden, devopsMutation.StatusCode);

        var ownerResponse = await api.PostJsonAsync("/api/v1/accounts", new { username = "snapshot-new-owner" });
        ownerResponse.EnsureSuccessStatusCode();
        var owner = await ReadJsonAsync(ownerResponse);
        var transfer = await api.PutJsonAsync(
            $"/api/v1/ai-accounts/{reviewer.AccountId}/owner",
            new { ownerAccountId = owner.GetProperty("account").GetProperty("id").GetGuid(), version = reviewer.Version });
        transfer.EnsureSuccessStatusCode();

        var page = await api.Client.GetFromJsonAsync<CommentPageResponse>($"/api/v1/comments/issue/{issue.Id}");
        var historical = Assert.Single(page!.Items, item => item.AuthorAccountId == reviewer.AccountId);
        Assert.Equal("reviewer_ai", historical.FixedRole);
        Assert.Equal("admin", historical.OwnerDisplayName);
    }

    private static async Task<AiBearer> CreateAiBearerAsync(
        AuthenticatedApiTestContext api,
        string username,
        string fixedRole)
    {
        var create = await api.PostJsonAsync("/api/v1/ai-accounts", new { username, fixedRole });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var account = created.GetProperty("account");
        var accountId = account.GetProperty("id").GetGuid();
        var enabled = await api.PutJsonAsync(
            $"/api/v1/ai-accounts/{accountId}/api-enabled",
            new { enabled = true, version = account.GetProperty("version").GetInt32() });
        enabled.EnsureSuccessStatusCode();
        var enabledAccount = await ReadJsonAsync(enabled);
        var token = await api.PostJsonAsync($"/api/v1/ai-accounts/{accountId}/token", new { });
        Assert.Equal(HttpStatusCode.Created, token.StatusCode);
        return new AiBearer(
            accountId,
            enabledAccount.GetProperty("version").GetInt32(),
            (await ReadJsonAsync(token)).GetProperty("token").GetString()!);
    }

    private static async Task<HttpResponseMessage> PostCommentAsync(
        HttpClient client,
        Guid issueId,
        string content,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/comments/issue/{issueId}")
        {
            Content = JsonContent.Create(new { content }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static HttpClient BearerClient(AuthenticatedApiTestContext api, string token)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, object content) =>
        client.PostAsJsonAsync(path, content);

    private static Task<HttpResponseMessage> PutJsonAsync(HttpClient client, string path, object content) =>
        client.PutAsJsonAsync(path, content);

    private static UpdateIssueRequest UpdateTask(
        IssueResponse task,
        string completionSummary,
        string? title = null) =>
        new(title ?? task.Title, task.TypeCode, task.PriorityCode, task.Version)
        {
            Description = task.Description,
            UserStory = task.UserStory,
            DefinitionOfDone = task.DefinitionOfDone,
            CompletionSummary = completionSummary,
        };

    private static CreateTestCaseRequest TestCaseRequest(Guid suiteId, string title) =>
        new(
            suiteId,
            title,
            [new CreateTestCaseStepRequest("Execute the test.", "The result is recorded.")])
        {
            SortOrder = 1,
        };

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.Clone();
    }

    private sealed record AiBearer(Guid AccountId, int Version, string Token);
}
