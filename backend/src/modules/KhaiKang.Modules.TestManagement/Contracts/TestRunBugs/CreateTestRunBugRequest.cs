namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record CreateTestRunBugRequest
{
    public required Guid ProjectId { get; init; }

    public required string Title { get; init; }

    public required string? PriorityCode { get; init; }

    public required string? Description { get; init; }

    public required Guid? AssigneeAccountId { get; init; }
}
