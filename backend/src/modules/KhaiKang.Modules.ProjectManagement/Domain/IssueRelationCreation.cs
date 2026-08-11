namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueRelationCreation
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required Guid RelationTypeId { get; init; }

    public required Guid SourceIssueId { get; init; }

    public required Guid TargetIssueId { get; init; }
}
