namespace KhaiKang.Modules.ProjectManagement.Contracts;

public interface IIssueCommentContextDirectory
{
    Task<bool> IsReadableAsync(
        ReadableIssueCommentContextQuery query,
        CancellationToken cancellationToken);

    Task<bool> IsAssignedTestIssueAsync(
        AssignedTestIssueCommentContextQuery query,
        CancellationToken cancellationToken);
}
