using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.Identity.Application.Comments;

public sealed class CommentActorDirectory(IdentityDbContext dbContext) : ICommentActorDirectory
{
    public Task<CommentActorSnapshot?> FindActiveAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.Id == accountId && account.Status == AccountStatus.Active)
            .Select(account => new CommentActorSnapshot
            {
                AccountId = account.Id,
                DisplayName = account.Username,
                AccountType = account.AccountType == AccountType.Human ? "human" : "ai_agent",
            })
            .SingleOrDefaultAsync(cancellationToken);
}
