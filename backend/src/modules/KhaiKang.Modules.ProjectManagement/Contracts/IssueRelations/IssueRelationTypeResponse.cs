namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueRelationTypeResponse
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string ForwardLabel { get; init; }

    public required string ReverseLabel { get; init; }

    public required string DirectionKind { get; init; }
}
