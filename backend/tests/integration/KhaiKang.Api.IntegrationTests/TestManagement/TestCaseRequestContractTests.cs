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
            suiteId: suiteId,
            title: "Contract case",
            steps:
            [
                new CreateTestCaseStepRequest(
                    action: "Perform the operation.",
                    expectedResult: "The result is visible."),
            ])
        {
            SortOrder = 0,
            TagIds = [tagId],
        };

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
            suiteId: suiteId,
            title: "Updated contract case",
            steps:
            [
                new CreateTestCaseStepRequest(
                    action: "Perform the updated operation.",
                    expectedResult: "The updated result is visible."),
            ])
        {
            SortOrder = 2,
            Status = "active",
            Version = 3,
            TagIds = [tagId],
        };

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
    [InlineData("suiteId")]
    [InlineData("title")]
    [InlineData("steps")]
    public async Task CreateCase_WhenEndpointValidatedFieldIsMissing_ReturnsValidationProblem(
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
    public async Task CreateCase_WhenRequiredSortOrderIsMissing_ReturnsBadRequest()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            CreateJson(suite.Id, omittedField: "sortOrder"));

        await AssertBadRequestProblemAsync(response);
    }

    [Theory]
    [InlineData("action", """[{"expectedResult":"The result is visible."}]""")]
    [InlineData("expectedResult", """[{"action":"Perform the operation."}]""")]
    public async Task CreateCase_WhenStepRequiredFieldIsMissing_ReturnsValidationProblem(
        string _,
        string stepsJson)
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            CreateJson(suite.Id, stepsJson: stepsJson));

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

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    public async Task CreateCase_WhenTagIdsAreOmittedOrNull_CreatesEmptyTagCollection(
        string? tagIdsJson)
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            CreateJson(suite.Id, tagIdsJson: tagIdsJson));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var testCase = await response.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(testCase);
        Assert.Empty(testCase.Tags);
        Assert.Null(testCase.Description);
        Assert.Null(testCase.Preconditions);
        Assert.Null(testCase.OverallExpectedResult);
    }

    [Theory]
    [InlineData("description")]
    [InlineData("preconditions")]
    [InlineData("overallExpectedResult")]
    public async Task CreateCase_WhenOptionalTextIsOmitted_CreatesNullValue(
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

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var testCase = await response.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(testCase);
        Assert.Null(testCase.Description);
        Assert.Null(testCase.Preconditions);
        Assert.Null(testCase.OverallExpectedResult);
    }

    [Fact]
    public async Task CreateCase_WhenTagIdsContainDuplicate_ReturnsValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);
        var tag = await CreateTagAsync(api);
        var duplicateTagIds = $"[\"{tag.Id}\",\"{tag.Id}\"]";

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/test-workspaces/{workspace.Id}/cases",
            CreateJson(suite.Id, tagIdsJson: duplicateTagIds));

        await AssertValidationProblemAsync(response, "tagIds");
    }

    [Theory]
    [InlineData("suiteId")]
    [InlineData("title")]
    [InlineData("steps")]
    public async Task UpdateCase_WhenEndpointValidatedFieldIsMissing_ReturnsValidationProblem(
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

    [Theory]
    [InlineData("sortOrder")]
    [InlineData("status")]
    [InlineData("version")]
    public async Task UpdateCase_WhenRequiredInitFieldIsMissing_ReturnsBadRequest(
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

        await AssertBadRequestProblemAsync(response);
    }

    [Fact]
    public async Task UpdateCase_WhenStatusIsNull_ReturnsValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var testCase = await ApiTestData.CreateCaseAsync(api, workspace.Id);
        var json = UpdateJson(testCase).Replace(
            "\"status\":\"active\"",
            "\"status\":null",
            StringComparison.Ordinal);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Put,
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}",
            json);

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

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    public async Task UpdateCase_WhenTagIdsAreOmittedOrNull_PreservesExistingTags(
        string? tagIdsJson)
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
            UpdateJson(testCase, tagIdsJson: tagIdsJson));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(updated);
        Assert.Equal(tag.Id, Assert.Single(updated.Tags).Id);
    }

    [Fact]
    public async Task UpdateCase_WhenTagIdsAreEmpty_ClearsExistingTags()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);
        var tag = await CreateTagAsync(api);
        var testCase = await CreateCaseWithTagAsync(api, workspace.Id, suite.Id, tag.Id);

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Put,
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}",
            UpdateJson(testCase, tagIdsJson: "[]"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(updated);
        Assert.Empty(updated.Tags);
    }

    [Theory]
    [InlineData("description")]
    [InlineData("preconditions")]
    [InlineData("overallExpectedResult")]
    public async Task UpdateCase_WhenOptionalTextIsOmitted_ClearsValue(
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(updated);
        Assert.Null(updated.Description);
        Assert.Null(updated.Preconditions);
        Assert.Null(updated.OverallExpectedResult);
    }

    [Fact]
    public async Task UpdateCase_WhenTagIdsContainDuplicate_ReturnsValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var workspace = await ApiTestData.CreateWorkspaceAsync(api);
        var suite = await CreateSuiteAsync(api, workspace.Id);
        var tag = await CreateTagAsync(api);
        var testCase = await CreateCaseWithTagAsync(api, workspace.Id, suite.Id, tag.Id);
        var duplicateTagIds = $"[\"{tag.Id}\",\"{tag.Id}\"]";

        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Put,
            $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}",
            UpdateJson(testCase, tagIdsJson: duplicateTagIds));

        await AssertValidationProblemAsync(response, "tagIds");
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
        string stepsJson = """[{"action":"Perform the updated operation.","expectedResult":"The updated result is visible."}]""",
        string? tagIdsJson = null)
        => UpdateJson(testCase.SuiteId, testCase.Version, omittedField, stepsJson, tagIdsJson);

    private static string UpdateJson(
        Guid suiteId,
        int version,
        string? omittedField = null,
        string stepsJson = """[{"action":"Perform the updated operation.","expectedResult":"The updated result is visible."}]""",
        string? tagIdsJson = null)
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
        if (tagIdsJson is not null)
        {
            properties.Add($"\"tagIds\":{tagIdsJson}");
        }

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
            new CreateTestSuiteRequest
            {
                ParentId = null,
                Name = "Contract suite",
                Description = null,
                SortOrder = 1,
            });
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

    private static async Task<TestCaseResponse> CreateCaseWithTagAsync(
        AuthenticatedApiTestContext api,
        Guid workspaceId,
        Guid suiteId,
        Guid tagId)
    {
        var response = await SendRawJsonAsync(
            api,
            HttpMethod.Post,
            $"/api/v1/test-workspaces/{workspaceId}/cases",
            CreateJson(suiteId, tagIdsJson: $"[\"{tagId}\"]"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<TestCaseResponse>(
            await response.Content.ReadFromJsonAsync<TestCaseResponse>());
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

    private static async Task AssertBadRequestProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }
}
