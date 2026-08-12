using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KhaiKang.Modules.TestManagement.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TestCaseRequestContractTests
{
    [Fact]
    public void CreateTestCaseRequest_DeserializesCanonicalJson()
    {
        var suiteId = Guid.Parse("fb703207-7bee-4ae5-a901-184b193bd93f");
        var request = JsonSerializer.Deserialize<CreateTestCaseRequest>(
            CreateJson(suiteId),
            JsonSerializerOptions.Web);

        Assert.NotNull(request);
        Assert.Equal(suiteId, request.SuiteId);
        Assert.Equal("Contract case", request.Title);
        Assert.Null(request.Description);
        Assert.Null(request.Preconditions);
        Assert.Null(request.OverallExpectedResult);
        Assert.Equal(0, request.SortOrder);
        Assert.Null(request.TagIds);
        var step = Assert.Single(request.Steps);
        Assert.Equal("Perform the operation.", step.Action);
        Assert.Equal("The result is visible.", step.ExpectedResult);
    }

    [Fact]
    public void CreateTestCaseRequest_UsesCanonicalCamelCaseJsonShape()
    {
        var suiteId = Guid.Parse("fb703207-7bee-4ae5-a901-184b193bd93f");
        var tagId = Guid.Parse("e289ca85-ff5d-4db7-af9d-e8f6fdd452a9");
        var request = new CreateTestCaseRequest(
            suiteId,
            "Contract case",
            null,
            null,
            null,
            0,
            [new("Perform the operation.", "The result is visible.")],
            TagIds: [tagId]);

        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(request, JsonSerializerOptions.Web));
        AssertCanonicalPropertyNames(document.RootElement);
        AssertCanonicalStepPropertyNames(document.RootElement.GetProperty("steps")[0]);
        Assert.Equal(tagId, document.RootElement.GetProperty("tagIds")[0].GetGuid());
    }

    [Fact]
    public void UpdateTestCaseRequest_UsesCanonicalCamelCaseJsonShape()
    {
        var suiteId = Guid.Parse("fb703207-7bee-4ae5-a901-184b193bd93f");
        var tagId = Guid.Parse("e289ca85-ff5d-4db7-af9d-e8f6fdd452a9");
        var request = new UpdateTestCaseRequest(
            suiteId,
            "Updated contract case",
            null,
            null,
            null,
            2,
            "active",
            3,
            [new("Perform the updated operation.", "The updated result is visible.")],
            TagIds: [tagId]);

        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(request, JsonSerializerOptions.Web));
        Assert.Equal(
            new[]
            {
                "suiteId",
                "title",
                "description",
                "preconditions",
                "overallExpectedResult",
                "sortOrder",
                "status",
                "version",
                "steps",
                "tagIds",
            }.Order(StringComparer.Ordinal),
            document.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
        Assert.Equal(tagId, document.RootElement.GetProperty("tagIds")[0].GetGuid());
        AssertCanonicalStepPropertyNames(document.RootElement.GetProperty("steps")[0]);
    }

    [Fact]
    public void UpdateTestCaseRequest_DeserializesCanonicalJson()
    {
        var suiteId = Guid.Parse("fb703207-7bee-4ae5-a901-184b193bd93f");
        var request = JsonSerializer.Deserialize<UpdateTestCaseRequest>(
            UpdateJson(suiteId, version: 3),
            JsonSerializerOptions.Web);

        Assert.NotNull(request);
        Assert.Equal(suiteId, request.SuiteId);
        Assert.Equal("Updated contract case", request.Title);
        Assert.Null(request.Description);
        Assert.Null(request.Preconditions);
        Assert.Null(request.OverallExpectedResult);
        Assert.Equal(1, request.SortOrder);
        Assert.Equal("active", request.Status);
        Assert.Equal(3, request.Version);
        Assert.Null(request.TagIds);
        var step = Assert.Single(request.Steps);
        Assert.Equal("Perform the updated operation.", step.Action);
        Assert.Equal("The updated result is visible.", step.ExpectedResult);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("steps")]
    public async Task CreateCase_WhenStableRequiredFieldIsMissing_ReturnsValidationProblem(
        string omittedField)
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            CreateJson(suite.Id, omittedField));

        await AssertValidationProblemAsync(response, "testCase");
    }

    [Fact]
    public async Task CreateCase_WhenStepIsNull_ReturnsValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            CreateJson(suite.Id, stepsJson: "[null]"));

        await AssertValidationProblemAsync(response, "testCase");
    }

    [Fact]
    public async Task CreateCase_WhenTagIdsAreOmitted_CreatesEmptyTagCollection()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            CreateJson(suite.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var testCase = await response.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(testCase);
        Assert.Empty(testCase.Tags);
        Assert.Null(testCase.Description);
        Assert.Null(testCase.Preconditions);
        Assert.Null(testCase.OverallExpectedResult);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("steps")]
    [InlineData("status")]
    [InlineData("version")]
    public async Task UpdateCase_WhenStableRequiredFieldIsMissing_ReturnsValidationProblem(
        string omittedField)
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Put,
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}",
            UpdateJson(testCase, omittedField));

        await AssertValidationProblemAsync(response, "testCase");
    }

    [Fact]
    public async Task UpdateCase_WhenStepIsNull_ReturnsValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Put,
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}",
            UpdateJson(testCase, stepsJson: "[null]"));

        await AssertValidationProblemAsync(response, "testCase");
    }

    [Fact]
    public async Task UpdateCase_WhenTagIdsAreOmitted_PreservesExistingTags()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);
        var tag = await CreateTagAsync(api);

        var createResponse = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            CreateJson(suite.Id, tagIdsJson: $"[\"{tag.Id}\"]"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var testCase = await createResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(testCase);
        Assert.Equal(tag.Id, Assert.Single(testCase.Tags).Id);

        var updateResponse = await SendRawJsonAsync(
            api,
            HttpMethod.Put,
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}",
            UpdateJson(testCase));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(updated);
        Assert.Equal(tag.Id, Assert.Single(updated.Tags).Id);
    }

    private static string CreateJson(
        Guid suiteId,
        string? omittedField = null,
        string stepsJson = """[{"action":"Perform the operation.","expectedResult":"The result is visible."}]""",
        string? tagIdsJson = null)
    {
        var properties = new List<string>
        {
            $"\"suiteId\":\"{suiteId}\"",
            "\"title\":\"Contract case\"",
            "\"description\":null",
            "\"preconditions\":null",
            "\"overallExpectedResult\":null",
            "\"sortOrder\":0",
            $"\"steps\":{stepsJson}",
        };
        if (tagIdsJson is not null)
        {
            properties.Add($"\"tagIds\":{tagIdsJson}");
        }

        return "{" + string.Join(",", properties.Where(
            property => !property.StartsWith($"\"{omittedField}\"", StringComparison.Ordinal))) + "}";
    }

    private static string UpdateJson(
        TestCaseResponse testCase,
        string? omittedField = null,
        string stepsJson = """[{"action":"Perform the updated operation.","expectedResult":"The updated result is visible."}]""")
        => UpdateJson(testCase.SuiteId, testCase.Version, omittedField, stepsJson);

    private static string UpdateJson(
        Guid suiteId,
        int version,
        string? omittedField = null,
        string stepsJson = """[{"action":"Perform the updated operation.","expectedResult":"The updated result is visible."}]""")
    {
        var properties = new List<string>
        {
            $"\"suiteId\":\"{suiteId}\"",
            "\"title\":\"Updated contract case\"",
            "\"description\":null",
            "\"preconditions\":null",
            "\"overallExpectedResult\":null",
            "\"sortOrder\":1",
            "\"status\":\"active\"",
            $"\"version\":{version}",
            $"\"steps\":{stepsJson}",
        };
        return "{" + string.Join(",", properties.Where(
            property => !property.StartsWith($"\"{omittedField}\"", StringComparison.Ordinal))) + "}";
    }

    private static void AssertCanonicalPropertyNames(JsonElement element)
    {
        Assert.Equal(
            new[]
            {
                "suiteId",
                "title",
                "description",
                "preconditions",
                "overallExpectedResult",
                "sortOrder",
                "steps",
                "tagIds",
            }.Order(StringComparer.Ordinal),
            element
                .EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }

    private static void AssertCanonicalStepPropertyNames(JsonElement element)
    {
        Assert.Equal(
            new[] { "action", "expectedResult" }.Order(StringComparer.Ordinal),
            element
                .EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }

    private static async Task<TestSuiteResponse> CreateSuiteAsync(
        AuthenticatedApiTestContext api,
        Guid workspaceId)
    {
        var response = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/suites",
            new CreateTestSuiteRequest(null, "Contract suite", null, 1));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<TestSuiteResponse>(
            await response.Content.ReadFromJsonAsync<TestSuiteResponse>());
    }

    private static async Task<TestTagResponse> CreateTagAsync(AuthenticatedApiTestContext api)
    {
        var response = await api.PostJsonAsync(
            "/api/v1/test-tags",
            new CreateTestTagRequest($"contract-{Guid.NewGuid():N}", null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<TestTagResponse>(
            await response.Content.ReadFromJsonAsync<TestTagResponse>());
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
