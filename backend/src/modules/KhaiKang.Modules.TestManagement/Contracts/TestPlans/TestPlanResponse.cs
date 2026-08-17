namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestPlanResponse
{
    public required Guid Id { get; init; }

    public required Guid WorkspaceId { get; init; }

    public required int PlanNo { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string? Description { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyList<TestPlanItemResponse> Items { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required int Version { get; init; }

    public required TestTraceIssueResponse? TestIssue { get; init; }
}
