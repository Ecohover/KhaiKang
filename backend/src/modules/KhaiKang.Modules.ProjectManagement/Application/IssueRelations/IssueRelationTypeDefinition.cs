namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record IssueRelationTypeDefinition
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string ForwardLabel { get; init; }

    public required string ReverseLabel { get; init; }

    public required string DirectionKind { get; init; }

    public required int SortOrder { get; init; }
}
