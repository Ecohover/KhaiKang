namespace KhaiKang.Modules.ProjectManagement.Contracts;

public interface IProjectDirectory
{
    Task<ProjectDirectoryEntry?> FindAccessibleAsync(
        Guid projectId,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, ProjectDirectoryEntry>> GetByIdsAsync(
        IReadOnlyCollection<Guid> projectIds,
        CancellationToken cancellationToken);
}
