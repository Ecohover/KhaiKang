using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.ProjectManagement.Application.Issues;

public sealed class IssueCommentContextDirectory(
    ProjectManagementDbContext dbContext,
    IIssueDirectory issueDirectory) : IIssueCommentContextDirectory
{
    public async Task<bool> IsReadableAsync(
        ReadableIssueCommentContextQuery query,
        CancellationToken cancellationToken)
    {
        var readable = await issueDirectory.FindReadableAsync(
            query.IssueId,
            query.OwnerScopeAccountId,
            cancellationToken);
        return readable?.ProjectId == query.ExpectedProjectId;
    }

    public async Task<bool> IsAssignedTestIssueAsync(
        AssignedTestIssueCommentContextQuery query,
        CancellationToken cancellationToken)
    {
        var commentable = await issueDirectory.FindCommentableAsync(
            query.IssueId,
            query.OwnerScopeAccountId,
            cancellationToken);
        if (commentable is null ||
            commentable.ProjectId != query.ExpectedProjectId ||
            commentable.TypeCode != "task")
        {
            return false;
        }

        return await dbContext.Issues
            .AsNoTracking()
            .Where(issue =>
                issue.Id == query.IssueId &&
                issue.ProjectId == query.ExpectedProjectId &&
                issue.AssigneeAccountId == query.AiActorAccountId)
            .AnyAsync(issue => dbContext.IssueRelations.Any(relation =>
                relation.ProjectId == query.ExpectedProjectId &&
                relation.RelationTypeId == IssueRelationCatalog.TestsId &&
                relation.SourceIssueId == issue.Id &&
                !relation.IsDeleted), cancellationToken);
    }
}
