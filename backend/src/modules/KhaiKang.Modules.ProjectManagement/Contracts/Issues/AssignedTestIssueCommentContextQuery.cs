namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record AssignedTestIssueCommentContextQuery
{
    public required Guid IssueId { get; init; }

    public required Guid ExpectedProjectId { get; init; }

    public required Guid AiActorAccountId { get; init; }

    public required Guid OwnerScopeAccountId { get; init; }
}
