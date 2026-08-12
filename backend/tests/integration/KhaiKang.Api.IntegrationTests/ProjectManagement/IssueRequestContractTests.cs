using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IssueRequestContractTests
{
    private const string MinimalCreateJson = """
        {
          "title": "Contract task",
          "typeCode": "task"
        }
        """;

    private const string FullUpdateJson = """
        {
          "title": "Updated contract task",
          "typeCode": "story",
          "priorityCode": "critical",
          "description": "Updated description",
          "userStory": "Updated user story",
          "definitionOfDone": "Updated completion criteria",
          "completionSummary": "Updated completion summary",
          "version": 7
        }
        """;

    [Fact]
    public void CreateIssueRequest_DeserializesMinimalCanonicalJson()
    {
        var request = JsonSerializer.Deserialize<CreateIssueRequest>(
            MinimalCreateJson,
            JsonSerializerOptions.Web);

        Assert.NotNull(request);
        Assert.Equal("Contract task", request.Title);
        Assert.Equal("task", request.TypeCode);
        Assert.Null(request.PriorityCode);
        Assert.Null(request.Description);
        Assert.Null(request.UserStory);
        Assert.Null(request.DefinitionOfDone);
        Assert.Null(request.AssigneeAccountId);
    }

    [Fact]
    public void UpdateIssueRequest_UsesCanonicalCamelCaseJsonShape()
    {
        var request = JsonSerializer.Deserialize<UpdateIssueRequest>(
            FullUpdateJson,
            JsonSerializerOptions.Web);

        Assert.NotNull(request);
        Assert.Equal("Updated contract task", request.Title);
        Assert.Equal("story", request.TypeCode);
        Assert.Equal("critical", request.PriorityCode);
        Assert.Equal("Updated description", request.Description);
        Assert.Equal("Updated user story", request.UserStory);
        Assert.Equal("Updated completion criteria", request.DefinitionOfDone);
        Assert.Equal("Updated completion summary", request.CompletionSummary);
        Assert.Equal(7, request.Version);

        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(request, JsonSerializerOptions.Web));
        Assert.Equal(
            [
                "title",
                "typeCode",
                "priorityCode",
                "description",
                "userStory",
                "definitionOfDone",
                "completionSummary",
                "version",
            ],
            document.RootElement.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public async Task CreateIssue_WhenOptionalFieldsAreOmitted_UsesDefaultPriority()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/projects/{project.Id}/issues",
            MinimalCreateJson);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var issue = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(issue);
        Assert.Equal("Contract task", issue.Title);
        Assert.Equal("task", issue.TypeCode);
        Assert.Equal("medium", issue.PriorityCode);
        Assert.Null(issue.Description);
        Assert.Null(issue.UserStory);
        Assert.Null(issue.DefinitionOfDone);
        Assert.Null(issue.AssigneeAccountId);
    }

    [Fact]
    public async Task CreateIssue_WhenTitleIsMissing_ReturnsTitleValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/projects/{project.Id}/issues",
            """{"typeCode":"task"}""");

        await AssertValidationProblemAsync(response, "Title");
    }

    [Fact]
    public async Task UpdateIssue_WhenVersionIsMissing_ReturnsVersionValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Contract task");

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Put,
            $"/api/v1/projects/{project.Id}/issues/{issue.Id}",
            """
            {
              "title": "Updated contract task",
              "typeCode": "task",
              "priorityCode": "medium"
            }
            """);

        await AssertValidationProblemAsync(response, "version");
    }

    private static async Task<HttpResponseMessage> SendRawJsonAsync(
        AuthenticatedApiTestContext api,
        HttpMethod method,
        string path,
        string json)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(
            "X-XSRF-TOKEN",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(api.Client));
        return await api.Client.SendAsync(request);
    }

    private static async Task AssertValidationProblemAsync(
        HttpResponseMessage response,
        string expectedError)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(expectedError, problem.Errors);
    }
}
