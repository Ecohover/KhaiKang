using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests.Identity;

public sealed class Pf01AiCommentScopeTests
{
    [Fact]
    public async Task CreateAndListComments_WithAiBearer_AdmitsAcceptedRoleTargetMatrix()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01ROLE");
        var pm = await CreateAiBearerAsync(api, "ai-pf01-pm", "pm_ai");
        var development = await CreateAiBearerAsync(api, "ai-pf01-development", "development_ai");
        var qa = await CreateAiBearerAsync(api, "ai-pf01-qa", "qa_ai");
        var reviewer = await CreateAiBearerAsync(api, "ai-pf01-reviewer", "reviewer_ai");
        var devops = await CreateAiBearerAsync(api, "ai-pf01-devops", "devops_ai");
        var story = await CreateIssueAsync(api, project.Id, "PF-01 handoff story", "story");
        var developmentTask = await CreateIssueAsync(
            api,
            project.Id,
            "PF-01 assigned development task",
            "task",
            development.AccountId);
        var testIssue = await CreateIssueAsync(
            api,
            project.Id,
            "PF-01 assigned test issue",
            "task",
            qa.AccountId);
        var reviewerTask = await CreateIssueAsync(
            api,
            project.Id,
            "PF-01 reviewer target",
            "task",
            development.AccountId);
        var devopsTask = await CreateIssueAsync(api, project.Id, "PF-01 diagnostic task", "task");
        await CreateTestsRelationAsync(api, project.Id, testIssue.Id, story.Id);
        var allowedRows = new[]
        {
            new AllowedCommentTarget(pm, story, "PM handoff evidence."),
            new AllowedCommentTarget(development, developmentTask, "Development completion evidence."),
            new AllowedCommentTarget(qa, testIssue, "QA test evidence."),
            new AllowedCommentTarget(reviewer, reviewerTask, "Reviewer feedback."),
            new AllowedCommentTarget(devops, devopsTask, "DevOps diagnostic evidence."),
        };

        foreach (var row in allowedRows)
        {
            using var bearer = BearerClient(api, row.Actor.Token);
            var path = IssueCommentPath(row.Target.Id);
            using var create = await PostCommentAsync(
                bearer,
                path,
                row.Content,
                $"role-allow-{row.Actor.FixedRole}-{Guid.NewGuid():N}");

            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<CommentResponse>();
            Assert.NotNull(created);
            Assert.Equal(row.Actor.AccountId, created.AuthorAccountId);

            using var list = await bearer.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var page = await list.Content.ReadFromJsonAsync<CommentPageResponse>();
            Assert.NotNull(page);
            Assert.True(page.CanCreate);
            Assert.Contains(page.Items, item =>
                item.Id == created.Id &&
                item.AuthorAccountId == row.Actor.AccountId &&
                item.Content == row.Content);
        }

        using var reviewerBearer = BearerClient(api, reviewer.Token);
        var deniedPath = IssueCommentPath(story.Id);
        var deniedKey = $"reviewer-denied-{Guid.NewGuid():N}";
        using var readable = await reviewerBearer.GetAsync(deniedPath);
        using var deniedCreate = await PostCommentAsync(
            reviewerBearer,
            deniedPath,
            "Reviewer must not comment on this story.",
            deniedKey);

        Assert.Equal(HttpStatusCode.OK, readable.StatusCode);
        var readablePage = await readable.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(readablePage);
        Assert.False(readablePage.CanCreate);
        Assert.Equal(HttpStatusCode.NotFound, deniedCreate.StatusCode);

        using var allowedAfterDenial = await PostCommentAsync(
            reviewerBearer,
            IssueCommentPath(reviewerTask.Id),
            "Reviewer can still comment on the corresponding task.",
            deniedKey);
        Assert.Equal(HttpStatusCode.Created, allowedAfterDenial.StatusCode);
    }

    [Fact]
    public async Task CreateComment_WhenRequiredRoleContextIsMissing_FailsClosedButRemainsReadable()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01CTX");
        var development = await CreateAiBearerAsync(api, "ai-pf01-context-development", "development_ai");
        var qa = await CreateAiBearerAsync(api, "ai-pf01-context-qa", "qa_ai");
        var reviewer = await CreateAiBearerAsync(api, "ai-pf01-context-reviewer", "reviewer_ai");
        var unassignedTask = await CreateIssueAsync(api, project.Id, "Unassigned development task", "task");
        var taskWithoutTestsRelation = await CreateIssueAsync(
            api,
            project.Id,
            "Assigned task without testing context",
            "task",
            qa.AccountId);
        var nonDevelopmentTarget = await CreateIssueAsync(
            api,
            project.Id,
            "Task assigned to QA instead of Development",
            "task",
            qa.AccountId);
        var deniedRows = new[]
        {
            new DeniedCommentTarget(development, unassignedTask),
            new DeniedCommentTarget(qa, taskWithoutTestsRelation),
            new DeniedCommentTarget(reviewer, nonDevelopmentTarget),
        };

        foreach (var row in deniedRows)
        {
            using var bearer = BearerClient(api, row.Actor.Token);
            var path = IssueCommentPath(row.Target.Id);
            using var list = await bearer.GetAsync(path);
            using var create = await PostCommentAsync(
                bearer,
                path,
                "Missing authoritative role context.",
                $"missing-context-{row.Actor.FixedRole}-{Guid.NewGuid():N}");

            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var page = await list.Content.ReadFromJsonAsync<CommentPageResponse>();
            Assert.NotNull(page);
            Assert.False(page.CanCreate);
            Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
        }
    }

    [Fact]
    public async Task QaAi_TestAssetComments_RequireAuthoritativeTestingContext()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01TEST");
        var story = await CreateIssueAsync(api, project.Id, "Testing requirement", "story");
        var qa = await CreateAiBearerAsync(api, "ai-pf01-test-assets", "qa_ai");
        var pm = await CreateAiBearerAsync(api, "ai-pf01-test-assets-pm", "pm_ai");
        var testIssue = await CreateIssueAsync(
            api,
            project.Id,
            "Assigned test issue",
            "task",
            qa.AccountId);
        await CreateTestsRelationAsync(api, project.Id, testIssue.Id, story.Id);
        var workspace = await ApiTestData.CreateWorkspaceAsync(api, "PF01TA");
        await ApiTestData.LinkProjectAsync(api, workspace.Id, project.Id);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id, "PF-01 sourced case");
        await CreateCaseSourceAsync(api, workspace.Id, testCase.Id, testIssue.Id);
        var plan = await ApiTestData.CreatePlanAsync(
            api,
            workspace.Id,
            testCase.Id,
            activate: true,
            testIssueId: testIssue.Id);
        var run = await ApiTestData.CreateRunAsync(api, workspace.Id, plan.Id, "PF-01 context run");
        var allowedTargets = new[]
        {
            new CommentTarget("test_case", testCase.Id),
            new CommentTarget("test_plan", plan.Id),
            new CommentTarget("test_run", run.Id),
        };

        using var qaBearer = BearerClient(api, qa.Token);
        foreach (var target in allowedTargets)
        {
            var path = target.Path;
            using var create = await PostCommentAsync(
                qaBearer,
                path,
                $"QA evidence for {target.ResourceType}.",
                $"qa-test-context-{target.ResourceType}-{Guid.NewGuid():N}");
            using var list = await qaBearer.GetAsync(path);

            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var page = await list.Content.ReadFromJsonAsync<CommentPageResponse>();
            Assert.NotNull(page);
            Assert.True(page.CanCreate);
            Assert.Contains(page.Items, item => item.AuthorAccountId == qa.AccountId);
        }

        var missingContextWorkspace = await ApiTestData.CreateWorkspaceAsync(api, "PF01MC");
        await ApiTestData.LinkProjectAsync(api, missingContextWorkspace.Id, project.Id);
        var missingContextCase = await ApiTestData.CreateCaseAsync(
            api,
            missingContextWorkspace.Id,
            "PF-01 unsourced case");
        var missingContextPath = new CommentTarget("test_case", missingContextCase.Id).Path;
        var deniedKey = $"qa-missing-context-{Guid.NewGuid():N}";
        using var missingList = await qaBearer.GetAsync(missingContextPath);
        using var missingCreate = await PostCommentAsync(
            qaBearer,
            missingContextPath,
            "Must not be created without a source Test Issue.",
            deniedKey);
        Assert.Equal(HttpStatusCode.OK, missingList.StatusCode);
        var missingPage = await missingList.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(missingPage);
        Assert.False(missingPage.CanCreate);
        Assert.Equal(HttpStatusCode.NotFound, missingCreate.StatusCode);

        using var allowedAfterDenial = await PostCommentAsync(
            qaBearer,
            allowedTargets[0].Path,
            "The denied key was not reserved.",
            deniedKey);
        Assert.Equal(HttpStatusCode.Created, allowedAfterDenial.StatusCode);

        using var pmBearer = BearerClient(api, pm.Token);
        using var pmList = await pmBearer.GetAsync(allowedTargets[0].Path);
        using var pmCreate = await PostCommentAsync(
            pmBearer,
            allowedTargets[0].Path,
            "PM cannot create a Test Case comment.",
            $"pm-test-denied-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.OK, pmList.StatusCode);
        var pmPage = await pmList.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(pmPage);
        Assert.False(pmPage.CanCreate);
        Assert.Equal(HttpStatusCode.NotFound, pmCreate.StatusCode);
    }

    [Fact]
    public async Task CommentAccess_AfterOwnerTransfer_UsesOnlyNewOwnerScopeAndDoesNotDiscloseMetadata()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01XFER");
        var task = await CreateIssueAsync(api, project.Id, "Private transfer target", "task");
        var devops = await CreateAiBearerAsync(api, "ai-pf01-transfer-devops", "devops_ai");
        using var originalBearer = BearerClient(api, devops.Token);
        var path = IssueCommentPath(task.Id);
        const string privateMarker = "PF01 private transfer comment";
        using var originalCreate = await PostCommentAsync(
            originalBearer,
            path,
            privateMarker,
            $"transfer-before-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, originalCreate.StatusCode);
        var newOwner = await CreateHumanAccountAsync(api, "pf01-new-owner");
        using var transfer = await api.PutJsonAsync(
            $"/api/v1/ai-accounts/{devops.AccountId}/owner",
            new { ownerAccountId = newOwner.AccountId, version = devops.Version });
        transfer.EnsureSuccessStatusCode();

        using var oldTokenList = await originalBearer.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, oldTokenList.StatusCode);

        using var newOwnerClient = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            newOwnerClient,
            newOwner.Username,
            newOwner.InitialPassword);
        using var recreate = await AuthenticatedApiTestContext.PostJsonAsync(
            newOwnerClient,
            $"/api/v1/ai-accounts/{devops.AccountId}/token",
            new { });
        recreate.EnsureSuccessStatusCode();
        var replacementToken = (await ReadJsonAsync(recreate)).GetProperty("token").GetString()!;
        using var transferredBearer = BearerClient(api, replacementToken);
        using var deniedExisting = await transferredBearer.GetAsync(path);
        using var deniedMissing = await transferredBearer.GetAsync(IssueCommentPath(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, deniedExisting.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deniedMissing.StatusCode);
        var deniedExistingBody = await deniedExisting.Content.ReadAsStringAsync();
        var deniedMissingBody = await deniedMissing.Content.ReadAsStringAsync();
        Assert.Equal(deniedMissingBody, deniedExistingBody);
        Assert.DoesNotContain(privateMarker, deniedExistingBody, StringComparison.Ordinal);
        Assert.DoesNotContain(task.Id.ToString(), deniedExistingBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(project.Id.ToString(), deniedExistingBody, StringComparison.OrdinalIgnoreCase);

        using var addMember = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new AddProjectMemberRequest(newOwner.Username, ["reviewer"]));
        Assert.Equal(HttpStatusCode.Created, addMember.StatusCode);
        using var allowedList = await transferredBearer.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
        var allowedPage = await allowedList.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(allowedPage);
        Assert.True(allowedPage.CanCreate);
        Assert.Contains(allowedPage.Items, item => item.Content == privateMarker);
        using var allowedCreate = await PostCommentAsync(
            transferredBearer,
            path,
            "New owner scope is active.",
            $"transfer-after-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, allowedCreate.StatusCode);

        var accounts = await api.Client.GetFromJsonAsync<KhaiKang.Modules.Identity.Contracts.AccountResponse[]>(
            "/api/v1/accounts");
        var currentOwner = Assert.Single(accounts!, account => account.Id == newOwner.AccountId);
        using var disableOwner = await api.PutJsonAsync(
            $"/api/v1/accounts/{newOwner.AccountId}/status",
            new { status = "disabled", version = currentOwner.Version });
        disableOwner.EnsureSuccessStatusCode();
        using var orphanedList = await transferredBearer.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, orphanedList.StatusCode);
    }

    [Fact]
    public async Task CommentAccess_WhenAiApiOrAccountIsDisabled_RejectsExistingBearer()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01OFF");
        var firstTask = await CreateIssueAsync(api, project.Id, "API disabled target", "task");
        var apiDisabledAi = await CreateAiBearerAsync(api, "ai-pf01-api-disabled", "devops_ai");
        using var apiDisabledBearer = BearerClient(api, apiDisabledAi.Token);
        using var beforeApiDisable = await apiDisabledBearer.GetAsync(IssueCommentPath(firstTask.Id));
        Assert.Equal(HttpStatusCode.OK, beforeApiDisable.StatusCode);
        using var disableApi = await api.PutJsonAsync(
            $"/api/v1/ai-accounts/{apiDisabledAi.AccountId}/api-enabled",
            new { enabled = false, version = apiDisabledAi.Version });
        disableApi.EnsureSuccessStatusCode();
        using var afterApiDisable = await apiDisabledBearer.GetAsync(IssueCommentPath(firstTask.Id));
        Assert.Equal(HttpStatusCode.Unauthorized, afterApiDisable.StatusCode);

        var secondTask = await CreateIssueAsync(api, project.Id, "Account disabled target", "task");
        var accountDisabledAi = await CreateAiBearerAsync(api, "ai-pf01-account-disabled", "devops_ai");
        using var accountDisabledBearer = BearerClient(api, accountDisabledAi.Token);
        using var beforeAccountDisable = await accountDisabledBearer.GetAsync(IssueCommentPath(secondTask.Id));
        Assert.Equal(HttpStatusCode.OK, beforeAccountDisable.StatusCode);
        var accounts = await api.Client.GetFromJsonAsync<KhaiKang.Modules.Identity.Contracts.AccountResponse[]>(
            "/api/v1/accounts");
        var currentAi = Assert.Single(accounts!, account => account.Id == accountDisabledAi.AccountId);
        using var disableAccount = await api.PutJsonAsync(
            $"/api/v1/accounts/{accountDisabledAi.AccountId}/status",
            new { status = "disabled", version = currentAi.Version });
        disableAccount.EnsureSuccessStatusCode();
        using var afterAccountDisable = await accountDisabledBearer.GetAsync(IssueCommentPath(secondTask.Id));
        Assert.Equal(HttpStatusCode.Unauthorized, afterAccountDisable.StatusCode);
    }

    [Fact]
    public async Task CommentAccess_WithHumanSession_PreservesReadCreateAndCsrfBoundaries()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01HUM");
        var task = await CreateIssueAsync(api, project.Id, "Human Comment target", "task");
        var path = IssueCommentPath(task.Id);

        using var humanList = await api.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, humanList.StatusCode);
        var humanPage = await humanList.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(humanPage);
        Assert.True(humanPage.CanCreate);
        using var humanCreate = await PostHumanCommentAsync(
            api.Client,
            path,
            "Human session Comment.",
            $"human-valid-{Guid.NewGuid():N}",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(api.Client));
        Assert.Equal(HttpStatusCode.Created, humanCreate.StatusCode);
        using var missingCsrf = await PostHumanCommentAsync(
            api.Client,
            path,
            "Missing CSRF must fail.",
            $"human-no-csrf-{Guid.NewGuid():N}",
            csrfToken: null);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        var devops = await CreateAiBearerAsync(api, "ai-pf01-human-boundary", "devops_ai");
        using var bearer = BearerClient(api, devops.Token);
        using var bearerCreate = await PostCommentAsync(
            bearer,
            path,
            "Bearer does not use CSRF.",
            $"bearer-no-csrf-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, bearerCreate.StatusCode);

        var outsider = await CreateHumanAccountAsync(api, "pf01-human-outsider");
        using var outsiderClient = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            outsiderClient,
            outsider.Username,
            outsider.InitialPassword);
        using var outsiderList = await outsiderClient.GetAsync(path);
        using var outsiderCreate = await PostHumanCommentAsync(
            outsiderClient,
            path,
            "Outsider Comment.",
            $"human-outsider-{Guid.NewGuid():N}",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(outsiderClient));
        Assert.Equal(HttpStatusCode.NotFound, outsiderList.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, outsiderCreate.StatusCode);
    }

    [Fact]
    public async Task CommentAccess_WhenSystemAdministratorOwnerLacksProjectScope_DoesNotBypassMembership()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01ADMIN");
        var task = await CreateIssueAsync(api, project.Id, "Private admin-bypass target", "task");
        var projectOwner = await CreateHumanAccountAsync(api, "pf01-project-owner");
        using var addOwner = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new AddProjectMemberRequest(projectOwner.Username, ["owner"]));
        Assert.Equal(HttpStatusCode.Created, addOwner.StatusCode);
        var members = await api.Client.GetFromJsonAsync<ProjectMemberResponse[]>(
            $"/api/v1/projects/{project.Id}/members");
        var adminMembership = Assert.Single(members!, member => member.Username == "admin");
        using var removeAdmin = await api.DeleteAsync(
            $"/api/v1/projects/{project.Id}/members/{adminMembership.Id}?version={adminMembership.Version}");
        Assert.Equal(HttpStatusCode.NoContent, removeAdmin.StatusCode);
        var devops = await CreateAiBearerAsync(api, "ai-pf01-admin-owner", "devops_ai");
        using var bearer = BearerClient(api, devops.Token);
        var path = IssueCommentPath(task.Id);

        using var list = await bearer.GetAsync(path);
        using var create = await PostCommentAsync(
            bearer,
            path,
            "System Administrator must not bypass scope.",
            $"admin-no-bypass-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
        var responseBody = await create.Content.ReadAsStringAsync();
        Assert.DoesNotContain(project.Name, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(task.Title, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QaAi_TestAssetComments_RequireOwnerProjectAndWorkspaceIntersection()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01INT");
        var story = await CreateIssueAsync(api, project.Id, "Intersection requirement", "story");
        var qa = await CreateAiBearerAsync(api, "ai-pf01-intersection", "qa_ai");
        var testIssue = await CreateIssueAsync(
            api,
            project.Id,
            "Intersection test issue",
            "task",
            qa.AccountId);
        await CreateTestsRelationAsync(api, project.Id, testIssue.Id, story.Id);
        var workspace = await ApiTestData.CreateWorkspaceAsync(api, "PF01IN");
        await ApiTestData.LinkProjectAsync(api, workspace.Id, project.Id);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id, "Intersection case");
        await CreateCaseSourceAsync(api, workspace.Id, testCase.Id, testIssue.Id);
        var newOwner = await CreateHumanAccountAsync(api, "pf01-intersection-owner");
        using var transfer = await api.PutJsonAsync(
            $"/api/v1/ai-accounts/{qa.AccountId}/owner",
            new { ownerAccountId = newOwner.AccountId, version = qa.Version });
        transfer.EnsureSuccessStatusCode();
        using var ownerClient = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            ownerClient,
            newOwner.Username,
            newOwner.InitialPassword);
        using var tokenResponse = await AuthenticatedApiTestContext.PostJsonAsync(
            ownerClient,
            $"/api/v1/ai-accounts/{qa.AccountId}/token",
            new { });
        tokenResponse.EnsureSuccessStatusCode();
        using var bearer = BearerClient(
            api,
            (await ReadJsonAsync(tokenResponse)).GetProperty("token").GetString()!);
        var path = new CommentTarget("test_case", testCase.Id).Path;

        using var projectMemberResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new AddProjectMemberRequest(newOwner.Username, ["reviewer"]));
        Assert.Equal(HttpStatusCode.Created, projectMemberResponse.StatusCode);
        var projectMember = Assert.IsType<ProjectMemberResponse>(
            await projectMemberResponse.Content.ReadFromJsonAsync<ProjectMemberResponse>());
        using var missingWorkspace = await bearer.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, missingWorkspace.StatusCode);

        using var workspaceMemberResponse = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspace.Id}/members",
            new AddTestWorkspaceMemberRequest(newOwner.Username, "tester"));
        Assert.Equal(HttpStatusCode.Created, workspaceMemberResponse.StatusCode);
        using var bothScopes = await bearer.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, bothScopes.StatusCode);
        var bothScopesPage = await bothScopes.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(bothScopesPage);
        Assert.True(bothScopesPage.CanCreate);

        using var removeProjectMember = await api.DeleteAsync(
            $"/api/v1/projects/{project.Id}/members/{projectMember.Id}?version={projectMember.Version}");
        Assert.Equal(HttpStatusCode.NoContent, removeProjectMember.StatusCode);
        using var missingProject = await bearer.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, missingProject.StatusCode);
    }

    private static async Task<AiBearer> CreateAiBearerAsync(
        AuthenticatedApiTestContext api,
        string username,
        string fixedRole)
    {
        using var create = await api.PostJsonAsync("/api/v1/ai-accounts", new { username, fixedRole });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var account = created.GetProperty("account");
        var accountId = account.GetProperty("id").GetGuid();
        using var enabled = await api.PutJsonAsync(
            $"/api/v1/ai-accounts/{accountId}/api-enabled",
            new { enabled = true, version = account.GetProperty("version").GetInt32() });
        enabled.EnsureSuccessStatusCode();
        var enabledAccount = await ReadJsonAsync(enabled);
        using var token = await api.PostJsonAsync($"/api/v1/ai-accounts/{accountId}/token", new { });
        Assert.Equal(HttpStatusCode.Created, token.StatusCode);

        return new AiBearer(
            accountId,
            enabledAccount.GetProperty("version").GetInt32(),
            (await ReadJsonAsync(token)).GetProperty("token").GetString()!,
            fixedRole);
    }

    private static async Task<HumanAccount> CreateHumanAccountAsync(
        AuthenticatedApiTestContext api,
        string username)
    {
        using var response = await api.PostJsonAsync("/api/v1/accounts", new { username });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        var account = created.GetProperty("account");
        return new HumanAccount(
            account.GetProperty("id").GetGuid(),
            account.GetProperty("username").GetString()!,
            created.GetProperty("initialPassword").GetString()!);
    }

    private static async Task<IssueResponse> CreateIssueAsync(
        AuthenticatedApiTestContext api,
        Guid projectId,
        string title,
        string typeCode,
        Guid? assigneeAccountId = null)
    {
        using var response = await api.PostJsonAsync(
            $"/api/v1/projects/{projectId}/issues",
            new CreateIssueRequest(title, typeCode)
            {
                AssigneeAccountId = assigneeAccountId,
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<IssueResponse>(await response.Content.ReadFromJsonAsync<IssueResponse>());
    }

    private static async Task CreateTestsRelationAsync(
        AuthenticatedApiTestContext api,
        Guid projectId,
        Guid testIssueId,
        Guid storyId)
    {
        using var response = await api.PostJsonAsync(
            $"/api/v1/projects/{projectId}/issues/{testIssueId}/relations",
            new CreateIssueRelationRequest("tests", storyId, "forward"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task CreateCaseSourceAsync(
        AuthenticatedApiTestContext api,
        Guid workspaceId,
        Guid caseId,
        Guid testIssueId)
    {
        using var response = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/cases/{caseId}/source-test-issue",
            new LinkTestCaseSourceRequest(testIssueId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostCommentAsync(
        HttpClient client,
        string path,
        string content,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { content }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostHumanCommentAsync(
        HttpClient client,
        string path,
        string content,
        string idempotencyKey,
        string? csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { content }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (csrfToken is not null)
        {
            request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        }
        return await client.SendAsync(request);
    }

    private static HttpClient BearerClient(AuthenticatedApiTestContext api, string token)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string IssueCommentPath(Guid issueId) => $"/api/v1/comments/issue/{issueId}";

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.Clone();
    }

    private sealed record AiBearer(Guid AccountId, int Version, string Token, string FixedRole);

    private sealed record HumanAccount(Guid AccountId, string Username, string InitialPassword);

    private sealed record AllowedCommentTarget(AiBearer Actor, IssueResponse Target, string Content);

    private sealed record DeniedCommentTarget(AiBearer Actor, IssueResponse Target);

    private sealed record CommentTarget(string ResourceType, Guid ResourceId)
    {
        public string Path => $"/api/v1/comments/{ResourceType}/{ResourceId}";
    }
}
