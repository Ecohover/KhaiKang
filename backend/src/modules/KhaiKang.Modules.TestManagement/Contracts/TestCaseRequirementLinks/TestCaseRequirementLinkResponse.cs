namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestCaseRequirementLinkResponse
{
    public required Guid Id { get; init; }

    public required Guid TestCaseId { get; init; }

    public required TestTraceIssueResponse Issue { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required int Version { get; init; }
}
