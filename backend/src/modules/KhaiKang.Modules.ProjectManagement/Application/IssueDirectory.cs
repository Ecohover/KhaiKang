using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed class IssueDirectory(ProjectManagementDbContext dbContext) : IIssueDirectory
{
    public Task<IssueDirectoryEntry?> FindReadableAsync(
        Guid issueId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return FindAsync(
            issueId,
            accountId,
            ProjectManagementConstants.IssueReadPermission,
            cancellationToken);
    }

    public Task<IssueDirectoryEntry?> FindUpdatableAsync(
        Guid issueId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return FindAsync(
            issueId,
            accountId,
            ProjectManagementConstants.IssueUpdatePermission,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, IssueDirectoryEntry>> GetReadableByIdsAsync(
        IReadOnlyCollection<Guid> issueIds,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (issueIds.Count == 0)
        {
            return new Dictionary<Guid, IssueDirectoryEntry>();
        }

        return await AuthorizedQuery(accountId, ProjectManagementConstants.IssueReadPermission)
            .Where(issue => issueIds.Contains(issue.Id))
            .Select(issue => new IssueDirectoryEntry(
                issue.Id,
                issue.ProjectId,
                issue.Project.Code,
                issue.Project.Status,
                issue.IssueNo,
                issue.Project.Code + "-" + issue.IssueNo,
                issue.Title,
                issue.IssueType.Code,
                issue.IssueStatus.Code))
            .ToDictionaryAsync(issue => issue.Id, cancellationToken);
    }

    private async Task<IssueDirectoryEntry?> FindAsync(
        Guid issueId,
        Guid accountId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return await AuthorizedQuery(accountId, permissionCode)
            .Where(issue => issue.Id == issueId)
            .Select(issue => new IssueDirectoryEntry(
                issue.Id,
                issue.ProjectId,
                issue.Project.Code,
                issue.Project.Status,
                issue.IssueNo,
                issue.Project.Code + "-" + issue.IssueNo,
                issue.Title,
                issue.IssueType.Code,
                issue.IssueStatus.Code))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Issue> AuthorizedQuery(Guid accountId, string permissionCode)
    {
        return dbContext.Issues.AsNoTracking().Where(issue =>
            dbContext.ProjectMembers.Any(member =>
                member.ProjectId == issue.ProjectId &&
                member.AccountId == accountId &&
                member.Status == ProjectMemberStatus.Active &&
                member.Roles.Any(mapping => mapping.ProjectRole.Permissions.Any(permission =>
                    permission.Permission.Code == permissionCode))));
    }
}
