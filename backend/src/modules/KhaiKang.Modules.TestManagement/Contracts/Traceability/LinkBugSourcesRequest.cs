namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record LinkBugSourcesRequest
{
    public Guid? TestIssueId { get; init; }
    public Guid? TestRunId { get; init; }
    public Guid? TestRunItemId { get; init; }
}
