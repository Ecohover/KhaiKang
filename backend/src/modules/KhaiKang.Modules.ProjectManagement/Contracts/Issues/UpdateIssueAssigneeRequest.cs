namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record UpdateIssueAssigneeRequest
{
    public required Guid? AssigneeAccountId { get; init; }

    public required int Version { get; init; }
}
