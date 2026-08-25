using KhaiKang.Modules.Identity.Application.AiAccounts;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.Identity.Application.ResourceActors;

public sealed class ResourceActorDirectory(IdentityDbContext dbContext) : IResourceActorDirectory
{
    public async Task<ResourceActorSnapshot?> FindActiveAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .AsNoTracking()
            .Include(item => item.OwnerAccount)
            .SingleOrDefaultAsync(
                item => item.Id == accountId && item.Status == AccountStatus.Active,
                cancellationToken);
        if (account is null ||
            account.AccountType == AccountType.AiAgent &&
            account.OwnerAccount is not { AccountType: AccountType.Human, Status: AccountStatus.Active })
        {
            return null;
        }

        var owner = account.OwnerAccount;
        return new ResourceActorSnapshot
        {
            AccountId = account.Id,
            ScopeAccountId = owner?.Id ?? account.Id,
            AccountName = account.Username,
            AccountType = account.AccountType == AccountType.Human ? "human" : "ai_agent",
            FixedRole = account.FixedRole is { } role ? AiFixedRoleCodes.ToCode(role) : null,
            OwnerAccountId = owner?.Id,
            OwnerDisplayName = owner?.Username,
        };
    }
}
