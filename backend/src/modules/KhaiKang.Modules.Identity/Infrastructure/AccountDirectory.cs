using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.Identity.Infrastructure;

internal sealed class AccountDirectory(IdentityDbContext dbContext) : IAccountDirectory
{
    public async Task<AccountDirectoryEntry?> FindActiveByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = username.Trim().ToUpperInvariant();
        return await dbContext.Accounts
            .AsNoTracking()
            .Where(account =>
                account.NormalizedUsername == normalizedUsername &&
                account.Status == AccountStatus.Active)
            .Select(account => new AccountDirectoryEntry
            {
                Id = account.Id,
                Username = account.Username,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, AccountDirectoryEntry>> GetByIdsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
        {
            return new Dictionary<Guid, AccountDirectoryEntry>();
        }

        return await dbContext.Accounts
            .AsNoTracking()
            .Where(account => accountIds.Contains(account.Id))
            .Select(account => new AccountDirectoryEntry
            {
                Id = account.Id,
                Username = account.Username,
            })
            .ToDictionaryAsync(account => account.Id, cancellationToken);
    }
}
