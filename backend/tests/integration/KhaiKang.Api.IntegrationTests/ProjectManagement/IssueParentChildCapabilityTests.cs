using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IssueParentChildCapabilityTests
{
    [Fact]
    public async Task CreateParentChild_FromStoryToTask_IsAllowed()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PARENTOK");
        var story = await ApiTestData.CreateIssueAsync(api, project.Id, "Parent story", "story");
        var task = await ApiTestData.CreateIssueAsync(api, project.Id, "Child task", "task");

        using var response = await CreateParentChildAsync(
            api,
            project.Id,
            story.Id,
            task.Id,
            $"parent-task-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var relation = await response.Content.ReadFromJsonAsync<IssueRelationResponse>();
        Assert.NotNull(relation);
        Assert.Equal("parent_of", relation.RelationTypeCode);
        Assert.Equal(story.Id, relation.SourceIssue.Id);
        Assert.Equal(task.Id, relation.TargetIssue.Id);
    }

    [Theory]
    [InlineData("task", "story")]
    [InlineData("story", "bug")]
    [InlineData("bug", "task")]
    public async Task CreateParentChild_WithDisallowedIssueTypes_IsRejected(
        string parentType,
        string childType)
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, $"BAD{Guid.NewGuid():N}"[..10]);
        var parent = await ApiTestData.CreateIssueAsync(api, project.Id, "Invalid parent", parentType);
        var child = await ApiTestData.CreateIssueAsync(api, project.Id, "Invalid child", childType);

        using var response = await CreateParentChildAsync(
            api,
            project.Id,
            parent.Id,
            child.Id,
            $"invalid-parent-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateParentChild_WhenChildAlreadyHasParent_ReturnsConflict()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PARENTS1");
        var firstStory = await ApiTestData.CreateIssueAsync(api, project.Id, "First parent", "story");
        var secondStory = await ApiTestData.CreateIssueAsync(api, project.Id, "Second parent", "story");
        var task = await ApiTestData.CreateIssueAsync(api, project.Id, "Single-parent child", "task");

        using var firstResponse = await CreateParentChildAsync(
            api,
            project.Id,
            firstStory.Id,
            task.Id,
            $"first-parent-{Guid.NewGuid():N}");
        using var secondResponse = await CreateParentChildAsync(
            api,
            project.Id,
            secondStory.Id,
            task.Id,
            $"second-parent-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateParentChild_AcrossProjects_ReturnsNotFound()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var parentProject = await ApiTestData.CreateProjectAsync(api, "PARENTP1");
        var childProject = await ApiTestData.CreateProjectAsync(api, "PARENTP2");
        var story = await ApiTestData.CreateIssueAsync(api, parentProject.Id, "Project-one parent", "story");
        var task = await ApiTestData.CreateIssueAsync(api, childProject.Id, "Project-two child", "task");

        using var response = await CreateParentChildAsync(
            api,
            parentProject.Id,
            story.Id,
            task.Id,
            $"cross-project-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateParentChild_WithSameIssueAsParentAndChild_ReturnsBadRequest()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PARENTSF");
        var story = await ApiTestData.CreateIssueAsync(api, project.Id, "Self parent", "story");

        using var response = await CreateParentChildAsync(
            api,
            project.Id,
            story.Id,
            story.Id,
            $"self-parent-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateParentChild_WhenExistingHierarchyWouldCycle_ReturnsConflict()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PARENTCY");
        var firstIssue = await ApiTestData.CreateIssueAsync(api, project.Id, "First hierarchy node", "story");
        var secondIssue = await ApiTestData.CreateIssueAsync(api, project.Id, "Second hierarchy node", "task");
        using var firstResponse = await CreateParentChildAsync(
            api,
            project.Id,
            firstIssue.Id,
            secondIssue.Id,
            $"cycle-first-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        await UpdateIssueTypeAsync(api, project.Id, firstIssue, "task");
        await UpdateIssueTypeAsync(api, project.Id, secondIssue, "story");

        using var cycleResponse = await CreateParentChildAsync(
            api,
            project.Id,
            secondIssue.Id,
            firstIssue.Id,
            $"cycle-second-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Conflict, cycleResponse.StatusCode);
        var problem = await cycleResponse.Content.ReadFromJsonAsync<ApiProblem>();
        Assert.NotNull(problem);
        Assert.Equal("issue_relation_hierarchy_cycle", problem.Code);
    }

    [Fact]
    public async Task DeleteParentChild_RemovesOnlyTheRelationAndKeepsBothIssues()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PARENTDL");
        var story = await ApiTestData.CreateIssueAsync(api, project.Id, "Persistent parent", "story");
        var task = await ApiTestData.CreateIssueAsync(api, project.Id, "Persistent child", "task");
        using var createResponse = await CreateParentChildAsync(
            api,
            project.Id,
            story.Id,
            task.Id,
            $"delete-parent-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var relation = await createResponse.Content.ReadFromJsonAsync<IssueRelationResponse>();
        Assert.NotNull(relation);

        using var deleteResponse = await DeleteParentChildAsync(
            api,
            project.Id,
            story.Id,
            relation.Id,
            relation.Version);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.NotNull(await api.Client.GetFromJsonAsync<IssueResponse>(
            $"/api/v1/projects/{project.Id}/issues/{story.Id}"));
        Assert.NotNull(await api.Client.GetFromJsonAsync<IssueResponse>(
            $"/api/v1/projects/{project.Id}/issues/{task.Id}"));
        var remainingRelations = await api.Client.GetFromJsonAsync<IReadOnlyList<IssueRelationResponse>>(
            $"/api/v1/projects/{project.Id}/issues/{story.Id}/relations");
        Assert.NotNull(remainingRelations);
        Assert.DoesNotContain(remainingRelations, item => item.Id == relation.Id);
    }

    [Fact]
    public async Task CreateParentChild_ReusingKey_ReplaysOrRejectsByFingerprint()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PARENTID");
        var story = await ApiTestData.CreateIssueAsync(api, project.Id, "Parent story", "story");
        var firstTask = await ApiTestData.CreateIssueAsync(api, project.Id, "First child", "task");
        var secondTask = await ApiTestData.CreateIssueAsync(api, project.Id, "Second child", "task");
        var key = $"parent-replay-{Guid.NewGuid():N}";

        using var firstResponse = await CreateParentChildAsync(
            api,
            project.Id,
            story.Id,
            firstTask.Id,
            key);
        using var replayResponse = await CreateParentChildAsync(
            api,
            project.Id,
            story.Id,
            firstTask.Id,
            key);
        using var conflictResponse = await CreateParentChildAsync(
            api,
            project.Id,
            story.Id,
            secondTask.Id,
            key);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<IssueRelationResponse>();
        var replay = await replayResponse.Content.ReadFromJsonAsync<IssueRelationResponse>();
        Assert.NotNull(first);
        Assert.NotNull(replay);
        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        var conflict = await conflictResponse.Content.ReadFromJsonAsync<ApiProblem>();
        Assert.NotNull(conflict);
        Assert.Equal("idempotency-conflict", conflict.Code);
    }

    private static async Task<HttpResponseMessage> CreateParentChildAsync(
        AuthenticatedApiTestContext api,
        Guid projectId,
        Guid parentIssueId,
        Guid childIssueId,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/projects/{projectId}/issues/{parentIssueId}/relations")
        {
            Content = JsonContent.Create(new CreateIssueRelationRequest(
                relationTypeCode: "parent_of",
                relatedIssueId: childIssueId,
                direction: "forward")),
        };
        request.Headers.Add(
            "X-XSRF-TOKEN",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(api.Client));
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await api.Client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> DeleteParentChildAsync(
        AuthenticatedApiTestContext api,
        Guid projectId,
        Guid parentIssueId,
        Guid relationId,
        int version)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/projects/{projectId}/issues/{parentIssueId}/relations/{relationId}?version={version}");
        request.Headers.Add(
            "X-XSRF-TOKEN",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(api.Client));
        return await api.Client.SendAsync(request);
    }

    private static async Task UpdateIssueTypeAsync(
        AuthenticatedApiTestContext api,
        Guid projectId,
        IssueResponse issue,
        string typeCode)
    {
        using var response = await api.PutJsonAsync(
            $"/api/v1/projects/{projectId}/issues/{issue.Id}",
            new UpdateIssueRequest(
                title: issue.Title,
                typeCode: typeCode,
                priorityCode: issue.PriorityCode,
                version: issue.Version));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record ApiProblem(string? Code);
}
