using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class CommentEndpointsTests
{
    [Fact]
    public async Task CreateAndListComments_AllSupportedResources_ReturnsPersistedComments()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CMTALL");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Comment target");
        var workspace = await ApiTestData.CreateWorkspaceAsync(api, "CMTALL");
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        var plan = await ApiTestData.CreatePlanAsync(api, workspace.Id, testCase.Id, activate: true);
        var run = await ApiTestData.CreateRunAsync(api, workspace.Id, plan.Id);
        var resources = new[]
        {
            new CommentResource("issue", issue.Id),
            new CommentResource("test_case", testCase.Id),
            new CommentResource("test_plan", plan.Id),
            new CommentResource("test_run", run.Id),
        };

        foreach (var resource in resources)
        {
            var content = $"Comment for {resource.Type}.";
            using var createResponse = await PostCommentAsync(
                api.Client,
                resource.Path,
                new { content },
                $"create-{resource.Type}-{Guid.NewGuid():N}");

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var created = await createResponse.Content.ReadFromJsonAsync<CommentResponse>();
            Assert.NotNull(created);
            Assert.Equal(content, created.Content);

            var page = await api.Client.GetFromJsonAsync<CommentPageResponse>(resource.Path);
            Assert.NotNull(page);
            Assert.Equal(created.Id, Assert.Single(page.Items).Id);
        }
    }

    [Fact]
    public async Task AccessComments_WithoutResourceAccess_DoesNotDiscloseTheResource()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CMTDENY");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Private comment target");
        await api.Factory.AddActiveAccountAsync("comment-outsider");
        using var outsider = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            outsider,
            "comment-outsider",
            AuthenticatedApiTestContext.TemporaryPassword);
        var path = $"/api/v1/comments/issue/{issue.Id}";

        using var listResponse = await outsider.GetAsync(path);
        using var createResponse = await PostCommentAsync(
            outsider,
            path,
            new { content = "Must not be accepted." },
            $"unauthorized-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);
    }

    [Fact]
    public async Task CreateIssueComment_WithCommentPermission_DoesNotRequireIssueUpdatePermission()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CMTPERM");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Reviewer comment target");
        await api.Factory.AddActiveAccountAsync("comment-reviewer");
        using var addMemberResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new AddProjectMemberRequest(
                username: "comment-reviewer",
                roleCodes: ["reviewer"]));
        Assert.Equal(HttpStatusCode.Created, addMemberResponse.StatusCode);
        using var reviewer = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            reviewer,
            "comment-reviewer",
            AuthenticatedApiTestContext.TemporaryPassword);
        var path = $"/api/v1/comments/issue/{issue.Id}";

        using var listResponse = await reviewer.GetAsync(path);
        using var createResponse = await PostCommentAsync(
            reviewer,
            path,
            new { content = "Review evidence." },
            $"review-comment-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    }

    [Fact]
    public async Task ListTestComments_ReturnsServerDerivedCreateAccessForWritableAndReadOnlyMembers()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api, "CMTACCESS");
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        var ownerPage = await api.Client.GetFromJsonAsync<CommentPageResponse>(
            $"/api/v1/comments/test_case/{testCase.Id}");
        Assert.NotNull(ownerPage);
        Assert.True(ownerPage.CanCreate);
        await api.Factory.AddActiveAccountAsync("comment-tester");
        using var addMemberResponse = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/members",
            new { username = "comment-tester", role = "tester" });
        Assert.Equal(HttpStatusCode.Created, addMemberResponse.StatusCode);
        using var tester = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            tester,
            "comment-tester",
            AuthenticatedApiTestContext.TemporaryPassword);

        var page = await tester.GetFromJsonAsync<CommentPageResponse>(
            $"/api/v1/comments/test_case/{testCase.Id}");

        Assert.NotNull(page);
        Assert.False(page.CanCreate);
    }

    [Fact]
    public async Task CreateComment_WithCallerSuppliedActorMetadata_UsesTheAuthenticatedActor()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CMTACTOR");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Actor source target");
        var forgedAccountId = Guid.NewGuid();
        var forgedTime = DateTimeOffset.Parse("2000-01-01T00:00:00Z");
        var beforeRequest = DateTimeOffset.UtcNow;

        using var response = await PostCommentAsync(
            api.Client,
            $"/api/v1/comments/issue/{issue.Id}",
            new
            {
                content = "Server-derived metadata.",
                authorAccountId = forgedAccountId,
                createdAt = forgedTime,
                accountType = "ai_agent",
            },
            $"actor-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(forgedAccountId, created.AuthorAccountId);
        Assert.NotEqual(forgedTime, created.CreatedAt);
        Assert.True(created.CreatedAt >= beforeRequest);
        Assert.Equal("human", created.AccountType);
    }

    [Fact]
    public async Task CreateComment_ReusingSuccessfulKey_ReplaysOrRejectsByFingerprint()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CMTIDEM");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Idempotent target");
        var path = $"/api/v1/comments/issue/{issue.Id}";
        var key = $"comment-success-{Guid.NewGuid():N}";

        using var firstResponse = await PostCommentAsync(
            api.Client,
            path,
            new { content = "Original payload." },
            key);
        using var replayResponse = await PostCommentAsync(
            api.Client,
            path,
            new { content = "Original payload." },
            key);
        using var conflictResponse = await PostCommentAsync(
            api.Client,
            path,
            new { content = "Substituted payload." },
            key);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<CommentResponse>();
        var replay = await replayResponse.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(first);
        Assert.NotNull(replay);
        Assert.Equal(first.Id, replay.Id);
        await AssertProblemCodeAsync(conflictResponse, HttpStatusCode.Conflict, "idempotency-conflict");

        var page = await api.Client.GetFromJsonAsync<CommentPageResponse>(path);
        Assert.NotNull(page);
        Assert.Equal(first.Id, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task CreateComment_ReusingRejectedKey_ReplaysTheFailureAndRejectsSubstitution()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CMTREJ");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Rejected comment target");
        var path = $"/api/v1/comments/issue/{issue.Id}";
        var key = $"comment-rejection-{Guid.NewGuid():N}";
        var rejectedPayload = new { content = new string('x', 10_001) };

        using var firstResponse = await PostCommentAsync(api.Client, path, rejectedPayload, key);
        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        using var replayResponse = await PostCommentAsync(api.Client, path, rejectedPayload, key);
        var replayBody = await replayResponse.Content.ReadAsStringAsync();
        using var conflictResponse = await PostCommentAsync(
            api.Client,
            path,
            new { content = "A different valid payload." },
            key);

        Assert.Equal(HttpStatusCode.BadRequest, firstResponse.StatusCode);
        Assert.Equal(firstResponse.StatusCode, replayResponse.StatusCode);
        Assert.Equal(firstBody, replayBody);
        await AssertProblemCodeAsync(conflictResponse, HttpStatusCode.Conflict, "idempotency-conflict");
    }

    [Fact]
    public async Task CreateComment_WithoutIdempotencyKey_IsRejected()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CMTKEY");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Missing key target");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/comments/issue/{issue.Id}")
        {
            Content = JsonContent.Create(new { content = "Missing idempotency key." }),
        };
        request.Headers.Add(
            "X-XSRF-TOKEN",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(api.Client));

        using var response = await api.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListComments_AcrossPages_UsesStableNewestFirstOrdering()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CMTPAGE");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Paged comment target");
        var path = $"/api/v1/comments/issue/{issue.Id}";

        for (var index = 0; index < 21; index++)
        {
            using var response = await PostCommentAsync(
                api.Client,
                path,
                new { content = $"Comment {index:D2}" },
                $"page-{index:D2}-{Guid.NewGuid():N}");
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var firstPage = await api.Client.GetFromJsonAsync<CommentPageResponse>(path);
        var secondPage = await api.Client.GetFromJsonAsync<CommentPageResponse>($"{path}?page=2");

        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(20, firstPage.Items.Count);
        Assert.Equal("Comment 20", firstPage.Items[0].Content);
        Assert.Equal("Comment 01", firstPage.Items[^1].Content);
        Assert.True(firstPage.HasNextPage);
        Assert.Equal("Comment 00", Assert.Single(secondPage.Items).Content);
        Assert.False(secondPage.HasNextPage);
    }

    [Fact]
    public async Task AccessComments_WithUnsupportedResourceType_ReturnsNotFound()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var resourceId = Guid.NewGuid();
        var path = $"/api/v1/comments/attachment/{resourceId}";

        using var listResponse = await api.Client.GetAsync(path);
        using var createResponse = await PostCommentAsync(
            api.Client,
            path,
            new { content = "Unsupported target." },
            $"unsupported-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostCommentAsync(
        HttpClient client,
        string path,
        object payload,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-XSRF-TOKEN", await AuthenticatedApiTestContext.GetCsrfTokenAsync(client));
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
        Assert.NotNull(problem);
        Assert.Equal(expectedCode, problem.Code);
    }

    private sealed record CommentResource(string Type, Guid Id)
    {
        public string Path => $"/api/v1/comments/{Type}/{Id}";
    }

    private sealed record CommentPageResponse(
        IReadOnlyList<CommentResponse> Items,
        int Page,
        bool HasNextPage,
        bool CanCreate);

    private sealed record CommentResponse(
        Guid Id,
        Guid AuthorAccountId,
        string? AccountType,
        string Content,
        DateTimeOffset CreatedAt,
        int Version);

    private sealed record ApiProblem(string? Code);
}
