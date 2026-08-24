using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.TestManagement.Domain;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.TestManagement.Application;

public sealed class TestCommentResourceDirectory(TestManagementDbContext dbContext) : ICommentResourceDirectory
{
    public bool Supports(CommentResourceType resourceType) => resourceType is CommentResourceType.TestCase or CommentResourceType.TestPlan or CommentResourceType.TestRun;

    public async Task<CommentResourceAccess> GetAccessAsync(CommentResourceType resourceType, Guid resourceId, Guid accountId, CancellationToken cancellationToken)
    {
        var workspaceId = resourceType switch
        {
            CommentResourceType.TestCase => await dbContext.Cases.Where(item => item.Id == resourceId).Select(item => (Guid?)item.TestWorkspaceId).SingleOrDefaultAsync(cancellationToken),
            CommentResourceType.TestPlan => await dbContext.Plans.Where(item => item.Id == resourceId).Select(item => (Guid?)item.TestWorkspaceId).SingleOrDefaultAsync(cancellationToken),
            CommentResourceType.TestRun => await dbContext.Runs.Where(item => item.Id == resourceId).Select(item => (Guid?)item.Plan.TestWorkspaceId).SingleOrDefaultAsync(cancellationToken),
            _ => null,
        };
        if (workspaceId is null) return Denied();
        var membership = await dbContext.Members.AsNoTracking().SingleOrDefaultAsync(item => item.TestWorkspaceId == workspaceId && item.AccountId == accountId && item.Status == TestWorkspaceMemberStatus.Active, cancellationToken);
        if (membership is null) return Denied();
        return new CommentResourceAccess
        {
            CanRead = true,
            CanCreate = membership.Role is TestWorkspaceRole.Owner or TestWorkspaceRole.Manager,
        };
    }

    private static CommentResourceAccess Denied() => new()
    {
        CanRead = false,
        CanCreate = false,
    };
}
