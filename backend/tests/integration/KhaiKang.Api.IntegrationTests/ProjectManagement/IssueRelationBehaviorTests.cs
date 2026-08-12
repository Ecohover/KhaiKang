using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IssueRelationBehaviorTests
{
    [Fact]
    public async Task CreateRelation_InvalidInputs_ReturnsValidationOrNotFound()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);
        var first = await ApiTestData.CreateIssueAsync(api, project.Id, "First task");
        var second = await ApiTestData.CreateIssueAsync(api, project.Id, "Second task");
        var path = $"/api/v1/projects/{project.Id}/issues/{first.Id}/relations";

        var invalidType = await api.PostJsonAsync(
            path,
            new CreateIssueRelationRequest("unknown", second.Id, "forward"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidType.StatusCode);

        var invalidDirection = await api.PostJsonAsync(
            path,
            new CreateIssueRelationRequest("related", second.Id, "sideways"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDirection.StatusCode);

        var selfRelation = await api.PostJsonAsync(
            path,
            new CreateIssueRelationRequest("related", first.Id, "forward"));
        Assert.Equal(HttpStatusCode.BadRequest, selfRelation.StatusCode);

        var missingTarget = await api.PostJsonAsync(
            path,
            new CreateIssueRelationRequest("related", Guid.NewGuid(), "forward"));
        Assert.Equal(HttpStatusCode.NotFound, missingTarget.StatusCode);
    }

    [Fact]
    public async Task MutateRelation_WhenProjectIsInactive_ReturnsProjectInactiveConflict()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);
        var first = await ApiTestData.CreateIssueAsync(api, project.Id, "First task");
        var second = await ApiTestData.CreateIssueAsync(api, project.Id, "Second task");
        var third = await ApiTestData.CreateIssueAsync(api, project.Id, "Third task");

        var createResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/issues/{first.Id}/relations",
            new CreateIssueRelationRequest("related", second.Id, "forward"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var relation = Assert.IsType<IssueRelationResponse>(
            await createResponse.Content.ReadFromJsonAsync<IssueRelationResponse>());

        var deactivateResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}",
            new UpdateProjectRequest(
                project.Name,
                project.Description,
                "inactive",
                project.Version));
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var blockedCreate = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/issues/{first.Id}/relations",
            new CreateIssueRelationRequest("related", third.Id, "forward"));
        await AssertConflictCodeAsync(blockedCreate, "project_inactive");

        var blockedDelete = await api.DeleteAsync(
            $"/api/v1/projects/{project.Id}/issues/{first.Id}/relations/{relation.Id}?version={relation.Version}");
        await AssertConflictCodeAsync(blockedDelete, "project_inactive");
    }

    [Fact]
    public async Task MutateRelation_WithoutRequiredPermissions_ReturnsForbidden()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);
        var first = await ApiTestData.CreateIssueAsync(api, project.Id, "First task");
        var second = await ApiTestData.CreateIssueAsync(api, project.Id, "Second task");

        var ownerCreateResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/issues/{first.Id}/relations",
            new CreateIssueRelationRequest("related", second.Id, "forward"));
        Assert.Equal(HttpStatusCode.Created, ownerCreateResponse.StatusCode);
        var relation = Assert.IsType<IssueRelationResponse>(
            await ownerCreateResponse.Content.ReadFromJsonAsync<IssueRelationResponse>());

        await api.Factory.AddActiveAccountAsync("relation-reviewer");
        var addReviewerResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new AddProjectMemberRequest(
                username: "relation-reviewer",
                roleCodes: ["reviewer"]));
        Assert.Equal(HttpStatusCode.Created, addReviewerResponse.StatusCode);

        using var reviewerClient = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            reviewerClient,
            "relation-reviewer",
            AuthenticatedApiTestContext.TemporaryPassword);

        var typesResponse = await reviewerClient.GetAsync(
            $"/api/v1/projects/{project.Id}/issue-relation-types");
        Assert.Equal(HttpStatusCode.OK, typesResponse.StatusCode);

        var blockedCreate = await AuthenticatedApiTestContext.PostJsonAsync(
            reviewerClient,
            $"/api/v1/projects/{project.Id}/issues/{first.Id}/relations",
            new CreateIssueRelationRequest("blocks", second.Id, "forward"));
        Assert.Equal(HttpStatusCode.Forbidden, blockedCreate.StatusCode);

        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/projects/{project.Id}/issues/{first.Id}/relations/{relation.Id}?version={relation.Version}");
        deleteRequest.Headers.Add(
            "X-XSRF-TOKEN",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(reviewerClient));
        var blockedDelete = await reviewerClient.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.Forbidden, blockedDelete.StatusCode);
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
