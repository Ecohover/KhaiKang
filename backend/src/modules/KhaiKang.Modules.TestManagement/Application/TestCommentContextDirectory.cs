using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.TestManagement.Application;

internal sealed class TestCommentContextDirectory(TestManagementDbContext dbContext)
{
    public Task<TestCommentContext?> FindAsync(
        CommentResourceType resourceType,
        Guid resourceId,
        CancellationToken cancellationToken) => resourceType switch
        {
            CommentResourceType.TestCase => dbContext.CaseSourceLinks
                .AsNoTracking()
                .Where(item => item.TestCaseId == resourceId)
                .Select(item => new TestCommentContext(
                    item.TestWorkspaceId,
                    item.ProjectId,
                    item.TestIssueId))
                .SingleOrDefaultAsync(cancellationToken),
            CommentResourceType.TestPlan => dbContext.Plans
                .AsNoTracking()
                .Where(item =>
                    item.Id == resourceId &&
                    item.TestIssueProjectId != null &&
                    item.TestIssueId != null)
                .Select(item => new TestCommentContext(
                    item.TestWorkspaceId,
                    item.TestIssueProjectId!.Value,
                    item.TestIssueId!.Value))
                .SingleOrDefaultAsync(cancellationToken),
            CommentResourceType.TestRun => dbContext.Runs
                .AsNoTracking()
                .Where(item =>
                    item.Id == resourceId &&
                    item.TestIssueProjectId != null &&
                    item.TestIssueId != null)
                .Select(item => new TestCommentContext(
                    item.Plan.TestWorkspaceId,
                    item.TestIssueProjectId!.Value,
                    item.TestIssueId!.Value))
                .SingleOrDefaultAsync(cancellationToken),
            _ => Task.FromResult<TestCommentContext?>(null),
        };
}
