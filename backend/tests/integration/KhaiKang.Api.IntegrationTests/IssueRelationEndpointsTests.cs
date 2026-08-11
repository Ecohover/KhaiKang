using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IssueRelationEndpointsTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

    [Fact]
    public async Task RelationFlow_PreservesDirectionAndRejectsDuplicateParentAndCycle()
    {
        var csrfToken = await GetCsrfTokenAsync();
        var initializeResponse = await PostAsync("/api/v1/setup/initialize", null, csrfToken);
        initializeResponse.EnsureSuccessStatusCode();
        var credentials = await initializeResponse.Content.ReadFromJsonAsync<InitializeAdminResponse>();
        Assert.NotNull(credentials);

        var loginResponse = await PostAsync(
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest("admin", credentials.InitialPassword, false)),
            csrfToken);
        loginResponse.EnsureSuccessStatusCode();

        var projectResponse = await PostAsync(
            "/api/v1/projects",
            JsonContent.Create(new CreateProjectRequest("trace", "Traceability", null)),
            await GetCsrfTokenAsync());
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);

        var requirement = await CreateIssueAsync(project.Id, "Requirement", "story");
        var testTask = await CreateIssueAsync(project.Id, "Test task", "task");
        var childTask = await CreateIssueAsync(project.Id, "Child task", "task");

        var types = await _client.GetFromJsonAsync<IssueRelationTypeResponse[]>(
            $"/api/v1/projects/{project.Id}/issue-relation-types");
        Assert.NotNull(types);
        Assert.Equal(5, types.Length);
        Assert.Contains(types, item => item.Code == "tests" && item.DirectionKind == "directed");
        Assert.Contains(types, item => item.Code == "parent_of" && item.DirectionKind == "hierarchical");

        var testsRelation = await CreateRelationAsync(
            project.Id,
            testTask.Id,
            "tests",
            requirement.Id,
            "forward",
            HttpStatusCode.Created);
        Assert.NotNull(testsRelation);
        Assert.Equal(testTask.Id, testsRelation.SourceIssue.Id);
        Assert.Equal(requirement.Id, testsRelation.TargetIssue.Id);

        var requirementRelationsResponse = await _client.GetAsync(
            $"/api/v1/projects/{project.Id}/issues/{requirement.Id}/relations");
        Assert.True(
            requirementRelationsResponse.IsSuccessStatusCode,
            await requirementRelationsResponse.Content.ReadAsStringAsync());
        var requirementRelations = await requirementRelationsResponse.Content
            .ReadFromJsonAsync<IssueRelationResponse[]>();
        Assert.NotNull(requirementRelations);
        var reverseVisible = Assert.Single(requirementRelations);
        Assert.Equal("tests", reverseVisible.RelationTypeCode);
        Assert.Equal(testTask.Id, reverseVisible.SourceIssue.Id);
        Assert.Equal(requirement.Id, reverseVisible.TargetIssue.Id);

        var related = await CreateRelationAsync(
            project.Id,
            requirement.Id,
            "related",
            testTask.Id,
            "forward",
            HttpStatusCode.Created);
        Assert.NotNull(related);
        await CreateRelationAsync(
            project.Id,
            testTask.Id,
            "related",
            requirement.Id,
            "forward",
            HttpStatusCode.Conflict);

        await CreateRelationAsync(
            project.Id,
            requirement.Id,
            "parent_of",
            testTask.Id,
            "forward",
            HttpStatusCode.Created);
        await CreateRelationAsync(
            project.Id,
            childTask.Id,
            "parent_of",
            testTask.Id,
            "forward",
            HttpStatusCode.Conflict);
        await CreateRelationAsync(
            project.Id,
            testTask.Id,
            "parent_of",
            childTask.Id,
            "forward",
            HttpStatusCode.Created);
        await CreateRelationAsync(
            project.Id,
            childTask.Id,
            "parent_of",
            requirement.Id,
            "forward",
            HttpStatusCode.Conflict);

        var staleDelete = await DeleteAsync(
            $"/api/v1/projects/{project.Id}/issues/{requirement.Id}/relations/{testsRelation.Id}?version={testsRelation.Version + 1}",
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Conflict, staleDelete.StatusCode);

        var delete = await DeleteAsync(
            $"/api/v1/projects/{project.Id}/issues/{requirement.Id}/relations/{testsRelation.Id}?version={testsRelation.Version}",
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    private async Task<IssueResponse> CreateIssueAsync(Guid projectId, string title, string typeCode)
    {
        var response = await PostAsync(
            $"/api/v1/projects/{projectId}/issues",
            JsonContent.Create(new { title, typeCode }),
            await GetCsrfTokenAsync());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!;
    }

    private async Task<IssueRelationResponse?> CreateRelationAsync(
        Guid projectId,
        Guid issueId,
        string relationTypeCode,
        Guid relatedIssueId,
        string direction,
        HttpStatusCode expectedStatus)
    {
        var response = await PostAsync(
            $"/api/v1/projects/{projectId}/issues/{issueId}/relations",
            JsonContent.Create(new CreateIssueRelationRequest(
                relationTypeCode,
                relatedIssueId,
                direction)),
            await GetCsrfTokenAsync());
        Assert.Equal(expectedStatus, response.StatusCode);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<IssueRelationResponse>()
            : null;
    }

    private async Task<string> GetCsrfTokenAsync()
    {
        var response = await _client.GetFromJsonAsync<CsrfTokenResponse>("/api/v1/auth/csrf-token");
        Assert.NotNull(response);
        return response.Token;
    }

    private async Task<HttpResponseMessage> PostAsync(
        string path,
        HttpContent? content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAsync(string path, string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await _client.SendAsync(request);
    }
}
