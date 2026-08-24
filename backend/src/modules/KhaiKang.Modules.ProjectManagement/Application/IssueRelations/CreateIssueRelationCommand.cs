using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record CreateIssueRelationCommand
{
    public required Guid ProjectId { get; init; }
    public required Guid IssueId { get; init; }
    public required Guid ActorAccountId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required CreateIssueRelationRequest Request { get; init; }
}
