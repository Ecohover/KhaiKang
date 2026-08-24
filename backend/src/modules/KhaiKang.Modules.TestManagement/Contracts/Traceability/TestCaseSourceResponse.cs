namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestCaseSourceResponse
{
    public required Guid Id { get; init; }
    public required Guid TestCaseId { get; init; }
    public required TestTraceIssueResponse TestIssue { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
