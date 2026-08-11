namespace KhaiKang.Modules.ProjectManagement.Contracts;

public interface IIssueCommandService
{
    Task<IssueCommandResult> CreateAsync(
        Guid projectId,
        Guid accountId,
        IssueCommandRequest request,
        CancellationToken cancellationToken);
}

public sealed record IssueCommandRequest(
    string Title,
    string TypeCode,
    string? PriorityCode,
    string? Description,
    Guid? AssigneeAccountId);

public enum IssueCommandOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    InvalidOption,
    InvalidAssignee,
    ProjectInactive,
    Conflict,
}

public sealed record IssueCommandResult(
    IssueCommandOutcome Outcome,
    IssueDirectoryEntry? Issue = null);
