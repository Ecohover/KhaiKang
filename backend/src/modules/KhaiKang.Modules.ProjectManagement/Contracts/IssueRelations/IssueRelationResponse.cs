namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueRelationResponse
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string RelationTypeCode { get; init; }

    public required string ForwardLabel { get; init; }

    public required string ReverseLabel { get; init; }

    public required string DirectionKind { get; init; }

    public required IssueRelationIssueResponse SourceIssue { get; init; }

    public required IssueRelationIssueResponse TargetIssue { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required int Version { get; init; }
}
