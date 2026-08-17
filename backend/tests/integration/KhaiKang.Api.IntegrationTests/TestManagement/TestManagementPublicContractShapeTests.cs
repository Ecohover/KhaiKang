using System.Text.Json;
using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TestManagementPublicContractShapeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RequestContracts_SerializeWithCanonicalPropertyNames()
    {
        var cases = new (object Request, string[] Properties)[]
        {
            (
                new CreateTestWorkspaceRequest("Workspace")
                {
                    Prefix = "QA",
                    Description = null,
                },
                ["name", "prefix", "description"]),
            (
                new UpdateTestWorkspaceRequest("Workspace", "active", 1)
                {
                    Description = null,
                },
                ["name", "status", "version", "description"]),
            (new LinkTestWorkspaceProjectRequest(Guid.NewGuid()), ["projectId"]),
            (new AddTestWorkspaceMemberRequest("tester", "tester"), ["username", "role"]),
            (new UpdateTestWorkspaceMemberRequest("manager", 1), ["role", "version"]),
            (
                new CreateTestSuiteRequest
                {
                    ParentId = null,
                    Name = "Suite",
                    Description = null,
                    SortOrder = 1,
                },
                ["parentId", "name", "description", "sortOrder"]),
            (
                new UpdateTestSuiteRequest
                {
                    ParentId = null,
                    Name = "Suite",
                    Description = null,
                    SortOrder = 1,
                    Status = "active",
                    Version = 1,
                },
                ["parentId", "name", "description", "sortOrder", "status", "version"]),
            (new CreateTestTagRequest("smoke", null), ["name", "description"]),
            (
                new UpdateTestTagRequest
                {
                    Name = "smoke",
                    Description = null,
                    Status = "active",
                    Version = 1,
                },
                ["name", "description", "status", "version"]),
            (
                new CreateTestPlanRequest(null, [])
                {
                    Name = null,
                    TestIssueId = null,
                },
                ["description", "caseIds", "name", "testIssueId"]),
            (
                new UpdateTestPlanRequest
                {
                    Name = null,
                    Description = null,
                    Status = "draft",
                    Version = 1,
                    CaseIds = [],
                    TestIssueId = null,
                },
                ["description", "caseIds", "status", "version", "name", "testIssueId"]),
            (new LinkTestCaseRequirementIssueRequest(Guid.NewGuid()), ["requirementIssueId"]),
            (new CreateTestRunRequest(Guid.NewGuid(), "Run"), ["planId", "name"]),
            (
                new UpdateTestRunStatusRequest("in_progress", null, 1),
                ["status", "summary", "version"]),
            (
                new RecordTestResultRequest("passed", null, 1),
                ["status", "actualResult", "version"]),
        };

        foreach (var (request, properties) in cases)
        {
            var json = JsonSerializer.SerializeToElement(request, request.GetType(), JsonOptions);

            AssertPropertyNames(json, properties);
        }
    }

    [Theory]
    [MemberData(nameof(ResponseContracts))]
    public void ResponseContracts_SerializeWithCanonicalPropertyNames(
        Type responseType,
        string[] properties)
    {
        var response = Activator.CreateInstance(responseType);
        var json = JsonSerializer.SerializeToElement(response, responseType, JsonOptions);

        AssertPropertyNames(json, properties);
    }

    public static TheoryData<Type, string[]> ResponseContracts => new()
    {
        {
            typeof(TestWorkspaceResponse),
            ["id", "name", "prefix", "description", "status", "currentUserRole", "createdAt", "updatedAt", "version"]
        },
        {
            typeof(TestWorkspaceProjectResponse),
            ["id", "projectId", "code", "name", "status", "linkedAt", "version"]
        },
        {
            typeof(TestWorkspaceMemberResponse),
            ["id", "accountId", "username", "role", "status", "joinedAt", "version"]
        },
        {
            typeof(TestSuiteResponse),
            ["id", "parentId", "name", "description", "sortOrder", "status", "depth", "version"]
        },
        {
            typeof(TestTagResponse),
            ["id", "name", "description", "status", "version"]
        },
        {
            typeof(TestCaseStepResponse),
            ["id", "stepNo", "action", "expectedResult"]
        },
        {
            typeof(TestCaseResponse),
            ["id", "suiteId", "caseNo", "tags", "title", "description", "preconditions", "overallExpectedResult", "sortOrder", "status", "steps", "createdAt", "updatedAt", "version"]
        },
        {
            typeof(TestCaseAttachmentResponse),
            ["id", "testCaseId", "originalFileName", "contentType", "fileSize", "fileHash", "uploadedByAccountId", "uploadedByUsername", "createdAt"]
        },
        {
            typeof(TestPlanItemResponse),
            ["id", "caseId", "sortOrder", "caseTitle"]
        },
        {
            typeof(TestPlanResponse),
            ["id", "workspaceId", "planNo", "code", "name", "description", "status", "items", "createdAt", "updatedAt", "version", "testIssue"]
        },
        {
            typeof(TestRunStepResponse),
            ["id", "stepNo", "action", "expectedResult", "resultStatus", "actualResult", "executedByAccountId", "executedAt", "version"]
        },
        {
            typeof(TestRunItemResponse),
            ["id", "caseId", "sortOrder", "caseTitle", "caseDescription", "preconditions", "overallExpectedResult", "resultStatus", "actualResult", "executedByAccountId", "executedAt", "steps", "version"]
        },
        {
            typeof(TestRunProgressResponse),
            ["total", "notRun", "passed", "failed", "blocked", "skipped"]
        },
        {
            typeof(TestRunResponse),
            ["id", "planId", "runNo", "code", "name", "status", "startedByAccountId", "startedAt", "completedAt", "summary", "progress", "items", "createdAt", "updatedAt", "version", "testIssue"]
        },
        {
            typeof(TestRunItemAttachmentResponse),
            ["id", "testRunItemId", "originalFileName", "contentType", "fileSize", "fileHash", "uploadedByAccountId", "uploadedByUsername", "createdAt"]
        },
        {
            typeof(TestTraceIssueResponse),
            ["id", "projectId", "projectCode", "issueNo", "key", "title", "typeCode", "statusCode"]
        },
        {
            typeof(TestCaseRequirementLinkResponse),
            ["id", "testCaseId", "issue", "createdAt", "version"]
        },
        {
            typeof(TestRunBugLinkResponse),
            ["id", "testRunId", "issue", "createdAt", "version"]
        },
    };

    private static void AssertPropertyNames(JsonElement json, IReadOnlyCollection<string> expected)
    {
        var actual = json.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(expected.OrderBy(name => name), actual);
    }
}
