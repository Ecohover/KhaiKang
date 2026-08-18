namespace KhaiKang.Modules.ProjectManagement.Contracts;

public interface IIssueCommandService
{
    Task<CreateIssueCommandResult> CreateAsync(
        Guid projectId,
        Guid accountId,
        CreateIssueCommand command,
        CancellationToken cancellationToken);
}
