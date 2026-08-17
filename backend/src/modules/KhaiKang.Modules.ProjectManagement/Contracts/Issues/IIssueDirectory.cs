namespace KhaiKang.Modules.ProjectManagement.Contracts;

public interface IIssueDirectory
{
    Task<IssueDirectoryEntry?> FindReadableAsync(
        Guid issueId,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<IssueDirectoryEntry?> FindUpdatableAsync(
        Guid issueId,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, IssueDirectoryEntry>> GetReadableByIdsAsync(
        IReadOnlyCollection<Guid> issueIds,
        Guid accountId,
        CancellationToken cancellationToken);
}
