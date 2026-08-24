namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record BugSourcesResponse
{
    public required TestTraceIssueResponse Bug { get; init; }
    public required TestTraceIssueResponse? TestIssue { get; init; }
    public required Guid? TestRunId { get; init; }
    public required Guid? TestRunItemId { get; init; }
}
