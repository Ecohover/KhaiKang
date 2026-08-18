namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestRunResponse
{
    public required Guid Id { get; init; }

    public required Guid PlanId { get; init; }

    public required int RunNo { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }

    public required Guid StartedByAccountId { get; init; }

    public required DateTimeOffset? StartedAt { get; init; }

    public required DateTimeOffset? CompletedAt { get; init; }

    public required string? Summary { get; init; }

    public required TestRunProgressResponse Progress { get; init; }

    public required IReadOnlyList<TestRunItemResponse> Items { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required int Version { get; init; }

    public required TestTraceIssueResponse? TestIssue { get; init; }
}
