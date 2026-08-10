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
                member.Status == "active" &&
                member.Roles.Any(mapping => mapping.ProjectRole.Permissions.Any(permission =>
                    permission.Permission.Code == ProjectManagementConstants.ProjectReadPermission)))
            .Select(member => new ProjectDirectoryEntry(
                member.Project.Id,
                member.Project.Code,
                member.Project.Name,
                member.Project.Status == ProjectStatus.Active ? "active" : "inactive"))
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
            .Select(project => new ProjectDirectoryEntry(
                project.Id,
                project.Code,
                project.Name,
                project.Status == ProjectStatus.Active ? "active" : "inactive"))
            .ToDictionaryAsync(project => project.Id, cancellationToken);
    }
}
