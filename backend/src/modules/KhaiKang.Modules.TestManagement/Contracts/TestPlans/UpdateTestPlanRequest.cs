namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record UpdateTestPlanRequest
{
    public required string? Description { get; init; }

    public required IReadOnlyList<Guid> CaseIds { get; init; }

    public required string Status { get; init; }

    public required int Version { get; init; }

    public string? Name { get; init; }

    public Guid? TestIssueId { get; init; }
}
