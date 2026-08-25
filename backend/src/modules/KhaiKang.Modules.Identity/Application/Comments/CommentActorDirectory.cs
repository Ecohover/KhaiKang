using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.Identity.Application.AiAccounts;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.Identity.Application.Comments;

public sealed class CommentActorDirectory(IdentityDbContext dbContext) : ICommentActorDirectory
{
    public async Task<CommentActorSnapshot?> FindActiveAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .AsNoTracking()
            .Include(item => item.OwnerAccount)
            .Where(account => account.Id == accountId && account.Status == AccountStatus.Active)
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null ||
            account.AccountType == AccountType.AiAgent &&
            account.OwnerAccount is not { Status: AccountStatus.Active })
        {
            return null;
        }

        return new CommentActorSnapshot
        {
            AccountId = account.Id,
            DisplayName = account.Username,
            AccountType = account.AccountType == AccountType.Human ? "human" : "ai_agent",
            FixedRole = account.FixedRole is { } role ? AiFixedRoleCodes.ToCode(role) : null,
            OwnerAccountId = account.OwnerAccount?.Id,
            OwnerDisplayName = account.OwnerAccount?.Username,
        };
    }
}
