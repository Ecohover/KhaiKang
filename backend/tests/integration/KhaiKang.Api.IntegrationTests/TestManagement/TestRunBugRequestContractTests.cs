using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KhaiKang.Modules.TestManagement.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TestRunBugRequestContractTests
{
    [Fact]
    public void CreateTestRunBugRequest_DeserializesCanonicalJson()
    {
        var projectId = Guid.Parse("33969f65-0a8b-49e8-aac4-48407f217631");
        var assigneeAccountId = Guid.Parse("aa5d7a45-276d-4a59-9a93-e6f95aaed4f4");

        var request = JsonSerializer.Deserialize<CreateTestRunBugRequest>(
            CreateJson(projectId, assigneeAccountId: assigneeAccountId),
            JsonSerializerOptions.Web);

        Assert.NotNull(request);
        Assert.Equal(projectId, request.ProjectId);
        Assert.Equal("Checkout total is incorrect", request.Title);
        Assert.Equal("high", request.PriorityCode);
        Assert.Equal("The displayed total differs from the order total.", request.Description);
        Assert.Equal(assigneeAccountId, request.AssigneeAccountId);
    }

    [Fact]
    public void CreateTestRunBugRequest_UsesCanonicalCamelCaseJsonShape()
    {
        var projectId = Guid.Parse("33969f65-0a8b-49e8-aac4-48407f217631");
        var request = new CreateTestRunBugRequest
        {
            ProjectId = projectId,
            Title = "Checkout total is incorrect",
            PriorityCode = "high",
            Description = "The displayed total differs from the order total.",
            AssigneeAccountId = null,
        };

        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(request, JsonSerializerOptions.Web));

        Assert.Equal(
            new[]
            {
                "projectId",
                "title",
                "priorityCode",
                "description",
                "assigneeAccountId",
            }.Order(StringComparer.Ordinal),
            document.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("projectId")]
    [InlineData("title")]
    [InlineData("priorityCode")]
    [InlineData("description")]
    [InlineData("assigneeAccountId")]
    public async Task CreateRunBug_WhenRequiredFieldIsMissing_ReturnsBadRequest(
        string omittedField)
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var scope = await CreateRunScopeAsync(api);

        var response = await SendRawJsonAsync(
            api,
            $"/api/v1/test-workspaces/{scope.WorkspaceId}/runs/{scope.RunId}/bugs",
            CreateJson(scope.ProjectId, omittedField));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task CreateRunBug_WhenNullableFieldsAreExplicitNull_CreatesBug()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var scope = await CreateRunScopeAsync(api);

        var response = await SendRawJsonAsync(
            api,
            $"/api/v1/test-workspaces/{scope.WorkspaceId}/runs/{scope.RunId}/bugs",
            CreateJson(
                scope.ProjectId,
                priorityCodeJson: "null",
                descriptionJson: "null",
                assigneeAccountIdJson: "null"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<TestRunBugLinkResponse>();
        Assert.NotNull(link);
        Assert.Equal("bug", link.Issue.TypeCode);
    }

    [Fact]
    public async Task CreateRunBug_WhenAssigneeDoesNotExist_ReturnsInvalidAssigneeCode()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var scope = await CreateRunScopeAsync(api);

        var response = await SendRawJsonAsync(
            api,
            $"/api/v1/test-workspaces/{scope.WorkspaceId}/runs/{scope.RunId}/bugs",
            CreateJson(scope.ProjectId, assigneeAccountId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "bug_issue_assignee_invalid",
            problem.RootElement.GetProperty("code").GetString());
    }

    private static string CreateJson(
        Guid projectId,
        string? omittedField = null,
        Guid? assigneeAccountId = null,
        string priorityCodeJson = "\"high\"",
        string descriptionJson = "\"The displayed total differs from the order total.\"",
        string? assigneeAccountIdJson = null)
    {
        assigneeAccountIdJson ??= assigneeAccountId is null
            ? "null"
            : $"\"{assigneeAccountId}\"";
        var properties = new[]
        {
            $"\"projectId\":\"{projectId}\"",
            "\"title\":\"Checkout total is incorrect\"",
            $"\"priorityCode\":{priorityCodeJson}",
            $"\"description\":{descriptionJson}",
            $"\"assigneeAccountId\":{assigneeAccountIdJson}",
        };

        return "{" + string.Join(",", properties.Where(
            property => !property.StartsWith($"\"{omittedField}\"", StringComparison.Ordinal))) + "}";
    }

    private static async Task<RunScope> CreateRunScopeAsync(
        AuthenticatedApiTestContext api)
    {
        var project = await ApiTestData.CreateProjectAsync(api, $"BUG{Guid.NewGuid():N}"[..8]);
        var workspace = await ApiTestData.CreateWorkspaceAsync(api, $"B{Guid.NewGuid():N}"[..6]);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        await ApiTestData.LinkProjectAsync(api, workspace.Id, project.Id);
        var plan = await ApiTestData.CreatePlanAsync(
            api,
            workspace.Id,
            testCase.Id,
            activate: true);
        var run = await ApiTestData.CreateRunAsync(api, workspace.Id, plan.Id);
        return new RunScope(workspace.Id, run.Id, project.Id);
    }

    private static async Task<HttpResponseMessage> SendRawJsonAsync(
        AuthenticatedApiTestContext api,
        string path,
        string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(
            "X-XSRF-TOKEN",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(api.Client));
        return await api.Client.SendAsync(request);
    }

    private sealed record RunScope(Guid WorkspaceId, Guid RunId, Guid ProjectId);
}
