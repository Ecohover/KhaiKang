namespace KhaiKang.Modules.Identity.Contracts;

public interface IAccountDirectory
{
    Task<AccountDirectoryEntry?> FindActiveByUsernameAsync(
        string username,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, AccountDirectoryEntry>> GetByIdsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken);
}
