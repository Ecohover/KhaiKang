using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.TestManagement.Domain;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.TestManagement.Application;

internal sealed class TestCommentResourceDirectory(
    TestManagementDbContext dbContext,
    TestCommentContextDirectory contextDirectory,
    IIssueCommentContextDirectory issueCommentContextDirectory,
    TestCommentTargetAdmission targetAdmission) : ICommentResourceDirectory
{
    public bool Supports(CommentResourceType resourceType) => resourceType is CommentResourceType.TestCase or CommentResourceType.TestPlan or CommentResourceType.TestRun;

    public async Task<CommentResourceAccess> GetAccessAsync(
        CommentResourceAccessQuery query,
        CancellationToken cancellationToken)
    {
        var workspaceId = query.ResourceType switch
        {
            CommentResourceType.TestCase => await dbContext.Cases.Where(item => item.Id == query.ResourceId).Select(item => (Guid?)item.TestWorkspaceId).SingleOrDefaultAsync(cancellationToken),
            CommentResourceType.TestPlan => await dbContext.Plans.Where(item => item.Id == query.ResourceId).Select(item => (Guid?)item.TestWorkspaceId).SingleOrDefaultAsync(cancellationToken),
            CommentResourceType.TestRun => await dbContext.Runs.Where(item => item.Id == query.ResourceId).Select(item => (Guid?)item.Plan.TestWorkspaceId).SingleOrDefaultAsync(cancellationToken),
            _ => null,
        };
        if (workspaceId is null) return Denied();
        var membership = await dbContext.Members.AsNoTracking().SingleOrDefaultAsync(
            item => item.TestWorkspaceId == workspaceId &&
                item.AccountId == query.Actor.ScopeAccountId &&
                item.Status == TestWorkspaceMemberStatus.Active,
            cancellationToken);
        if (membership is null) return Denied();
        var context = await contextDirectory.FindAsync(
            query.ResourceType,
            query.ResourceId,
            cancellationToken);
        if (query.Actor.IsAi && context is not null)
        {
            var linked = await dbContext.WorkspaceProjects
                .AsNoTracking()
                .AnyAsync(item =>
                    item.TestWorkspaceId == context.WorkspaceId &&
                    item.ProjectId == context.ProjectId,
                    cancellationToken);
            var projectReadable = linked &&
                await issueCommentContextDirectory.IsReadableAsync(
                    new ReadableIssueCommentContextQuery
                    {
                        IssueId = context.TestIssueId,
                        ExpectedProjectId = context.ProjectId,
                        OwnerScopeAccountId = query.Actor.ScopeAccountId,
                    },
                    cancellationToken);
            if (!projectReadable)
            {
                return Denied();
            }
        }

        var canCreate = query.Actor.IsAi
            ? await targetAdmission.CanCreateAsync(query, context, cancellationToken)
            : membership.Role is TestWorkspaceRole.Owner or TestWorkspaceRole.Manager;
        return new CommentResourceAccess
        {
            CanRead = true,
            CanCreate = canCreate,
        };
    }

    private static CommentResourceAccess Denied() => new()
    {
        CanRead = false,
        CanCreate = false,
    };
}
