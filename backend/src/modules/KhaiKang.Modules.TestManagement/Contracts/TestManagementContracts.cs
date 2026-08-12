namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record CreateTestWorkspaceRequest(string Name, string? Prefix, string? Description);
public sealed record UpdateTestWorkspaceRequest(string Name, string? Description, string Status, int Version);
public sealed record TestWorkspaceResponse(
    Guid Id,
    string Name,
    string Prefix,
    string? Description,
    string Status,
    string CurrentUserRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version);

public sealed record LinkTestWorkspaceProjectRequest(Guid ProjectId);

public sealed record TestWorkspaceProjectResponse(
    Guid Id,
    Guid ProjectId,
    string Code,
    string Name,
    string Status,
    DateTimeOffset LinkedAt,
    int Version);

public sealed record AddTestWorkspaceMemberRequest(string Username, string Role);
public sealed record UpdateTestWorkspaceMemberRequest(string Role, int Version);
public sealed record TestWorkspaceMemberResponse(
    Guid Id,
    Guid AccountId,
    string Username,
    string Role,
    string Status,
    DateTimeOffset JoinedAt,
    int Version);

public sealed record CreateTestSuiteRequest(
    Guid? ParentId,
    string Name,
    string? Description,
    int SortOrder);
public sealed record UpdateTestSuiteRequest(
    Guid? ParentId,
    string Name,
    string? Description,
    int SortOrder,
    string Status,
    int Version);
public sealed record TestSuiteResponse(
    Guid Id,
    Guid? ParentId,
    string Name,
    string? Description,
    int SortOrder,
    string Status,
    int Depth,
    int Version);

public sealed record TestTagResponse(Guid Id, string Name, string? Description, string Status, int Version);

public sealed record CreateTestTagRequest(string Name, string? Description);

public sealed record UpdateTestTagRequest(string Name, string? Description, string Status, int Version);

public sealed record TestCaseStepResponse(
    Guid Id,
    int StepNo,
    string Action,
    string ExpectedResult);

public sealed record TestCaseResponse(
    Guid Id,
    Guid SuiteId,
    int CaseNo,
    IReadOnlyList<TestTagResponse> Tags,
    string Title,
    string? Description,
    string? Preconditions,
    string? OverallExpectedResult,
    int SortOrder,
    string Status,
    IReadOnlyList<TestCaseStepResponse> Steps,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version);

public sealed record CreateTestPlanRequest(
    string? Name,
    string? Description,
    IReadOnlyList<Guid> CaseIds,
    Guid? TestIssueId = null);

public sealed record UpdateTestPlanRequest(
    string? Name,
    string? Description,
    string Status,
    int Version,
    IReadOnlyList<Guid> CaseIds,
    Guid? TestIssueId = null);

public sealed record TestTraceIssueResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectCode,
    int IssueNo,
    string Key,
    string Title,
    string TypeCode,
    string StatusCode);

public sealed record LinkTestCaseRequirementIssueRequest(Guid RequirementIssueId);

public sealed record TestCaseRequirementLinkResponse(
    Guid Id,
    Guid TestCaseId,
    TestTraceIssueResponse Issue,
    DateTimeOffset CreatedAt,
    int Version);

public sealed record CreateTestRunBugRequest(
    Guid ProjectId,
    string Title,
    string? PriorityCode,
    string? Description,
    Guid? AssigneeAccountId);

public sealed record TestRunBugLinkResponse(
    Guid Id,
    Guid TestRunId,
    TestTraceIssueResponse Issue,
    DateTimeOffset CreatedAt,
    int Version);

public sealed record TestPlanItemResponse(
    Guid Id,
    Guid CaseId,
    int SortOrder,
    string CaseTitle);

public sealed record TestPlanResponse(
    Guid Id,
    Guid WorkspaceId,
    int PlanNo,
    string Code,
    string Name,
    string? Description,
    string Status,
    IReadOnlyList<TestPlanItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version,
    TestTraceIssueResponse? TestIssue = null);

public sealed record CreateTestRunRequest(Guid PlanId, string Name);
public sealed record UpdateTestRunStatusRequest(string Status, string? Summary, int Version);
public sealed record RecordTestResultRequest(string Status, string? ActualResult, int Version);

public sealed record TestRunStepResponse(
    Guid Id,
    int StepNo,
    string Action,
    string ExpectedResult,
    string ResultStatus,
    string? ActualResult,
    Guid? ExecutedByAccountId,
    DateTimeOffset? ExecutedAt,
    int Version);

public sealed record TestRunItemResponse(
    Guid Id,
    Guid CaseId,
    int SortOrder,
    string CaseTitle,
    string? CaseDescription,
    string? Preconditions,
    string? OverallExpectedResult,
    string ResultStatus,
    string? ActualResult,
    Guid? ExecutedByAccountId,
    DateTimeOffset? ExecutedAt,
    IReadOnlyList<TestRunStepResponse> Steps,
    int Version);

public sealed record TestRunProgressResponse(
    int Total,
    int NotRun,
    int Passed,
    int Failed,
    int Blocked,
    int Skipped);

public sealed record TestRunResponse(
    Guid Id,
    Guid PlanId,
    int RunNo,
    string Code,
    string Name,
    string Status,
    Guid StartedByAccountId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? Summary,
    TestRunProgressResponse Progress,
    IReadOnlyList<TestRunItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version,
    TestTraceIssueResponse? TestIssue = null);
