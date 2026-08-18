using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed class ProjectDirectory(ProjectManagementDbContext dbContext) : IProjectDirectory
{
    public async Task<ProjectDirectoryEntry?> FindAccessibleAsync(
        Guid projectId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers
            .AsNoTracking()
            .Where(member =>
                member.ProjectId == projectId &&
                member.AccountId == accountId &&
                member.Status == ProjectMemberStatus.Active &&
                member.Roles.Any(mapping => mapping.ProjectRole.Permissions.Any(permission =>
                    permission.Permission.Code == ProjectManagementConstants.ProjectReadPermission)))
            .Select(member => new ProjectDirectoryEntry
            {
                Id = member.Project.Id,
                Code = member.Project.Code,
                Name = member.Project.Name,
                Status = member.Project.Status,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ProjectDirectoryEntry>> GetByIdsAsync(
        IReadOnlyCollection<Guid> projectIds,
        CancellationToken cancellationToken)
    {
        if (projectIds.Count == 0)
        {
            return new Dictionary<Guid, ProjectDirectoryEntry>();
        }

        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => projectIds.Contains(project.Id))
            .Select(project => new ProjectDirectoryEntry
            {
                Id = project.Id,
                Code = project.Code,
                Name = project.Name,
                Status = project.Status,
            })
            .ToDictionaryAsync(project => project.Id, cancellationToken);
    }
}
