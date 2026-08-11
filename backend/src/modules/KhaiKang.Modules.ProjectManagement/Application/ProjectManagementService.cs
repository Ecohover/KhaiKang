using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed class ProjectManagementService(
    ProjectManagementDbContext dbContext,
    IAccountDirectory accountDirectory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ProjectResponse>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var memberships = await dbContext.ProjectMembers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(member => member.Project)
            .Include(member => member.Roles)
            .ThenInclude(role => role.ProjectRole)
            .ThenInclude(role => role.Permissions)
            .ThenInclude(permission => permission.Permission)
            .Where(member =>
                member.AccountId == accountId &&
                member.Status == ProjectMemberStatus.Active &&
                member.Roles.Any(role => role.ProjectRole.Permissions.Any(permission =>
                    permission.Permission.Code == ProjectManagementConstants.ProjectReadPermission)))
            .OrderBy(member => member.Project.Name)
            .ToArrayAsync(cancellationToken);

        return memberships.Select(ToResponse).ToArray();
    }

    public async Task<ProjectResponse?> GetAsync(
        Guid projectId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.ProjectMembers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(member => member.Project)
            .Include(member => member.Roles)
            .ThenInclude(role => role.ProjectRole)
            .ThenInclude(role => role.Permissions)
            .ThenInclude(permission => permission.Permission)
            .Where(member =>
                member.ProjectId == projectId &&
                member.AccountId == accountId &&
                member.Status == ProjectMemberStatus.Active &&
                member.Roles.Any(role => role.ProjectRole.Permissions.Any(permission =>
                    permission.Permission.Code == ProjectManagementConstants.ProjectReadPermission)))
            .SingleOrDefaultAsync(cancellationToken);

        return membership is null ? null : ToResponse(membership);
    }

    public async Task<CreateProjectResult> CreateAsync(
        Guid accountId,
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Projects.AnyAsync(
            project => project.Code == normalizedCode,
            cancellationToken))
        {
            return new CreateProjectResult(CreateProjectOutcome.CodeConflict);
        }

        var now = timeProvider.GetUtcNow();
        var project = Project.Create(
            new ProjectCreation
            {
                Id = Guid.NewGuid(),
                Code = normalizedCode,
                Name = request.Name.Trim(),
                Description = NormalizeDescription(request.Description),
            },
            new ChangeContext(accountId, now));
        var member = new ProjectMember(Guid.NewGuid(), project.Id, accountId, now, accountId);
        var ownerRole = await dbContext.ProjectRoles
            .Include(role => role.Permissions)
            .ThenInclude(permission => permission.Permission)
            .SingleAsync(
                role => role.Code == ProjectManagementConstants.OwnerRoleCode,
                cancellationToken);
        var memberRole = new ProjectMemberRole(
            Guid.NewGuid(),
            member.Id,
            ownerRole.Id,
            now,
            accountId);

        dbContext.Projects.Add(project);
        dbContext.ProjectMembers.Add(member);
        dbContext.ProjectMemberRoles.Add(memberRole);
        dbContext.ProjectAuditEvents.Add(new ProjectAuditEvent(
            Guid.NewGuid(),
            accountId,
            "project_created",
            now,
            project.Id));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new CreateProjectResult(CreateProjectOutcome.CodeConflict);
        }

        return new CreateProjectResult(
            CreateProjectOutcome.Succeeded,
            ToResponse(
                project,
                [ownerRole.Name],
                ownerRole.Permissions.Select(permission => permission.Permission.Code)
                    .Order()
                    .ToArray()));
    }

    public async Task<UpdateProjectResult> UpdateAsync(
        Guid projectId,
        Guid accountId,
        UpdateProjectRequest request,
        bool canChangeStatus,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.ProjectMembers
            .Include(member => member.Project)
            .Include(member => member.Roles)
            .ThenInclude(role => role.ProjectRole)
            .ThenInclude(role => role.Permissions)
            .ThenInclude(permission => permission.Permission)
            .SingleOrDefaultAsync(
                member =>
                    member.ProjectId == projectId &&
                    member.AccountId == accountId &&
                    member.Status == ProjectMemberStatus.Active,
                cancellationToken);
        if (membership is null)
        {
            return new UpdateProjectResult(UpdateProjectOutcome.NotFound);
        }

        var permissions = membership.Roles
            .SelectMany(role => role.ProjectRole.Permissions)
            .Select(permission => permission.Permission.Code)
            .Distinct()
            .Order()
            .ToArray();
        if (!permissions.Contains(ProjectManagementConstants.ProjectUpdatePermission))
        {
            return new UpdateProjectResult(UpdateProjectOutcome.Forbidden);
        }

        var requestedStatus = ProjectManagementCodes.ParseProjectStatus(request.Status);
        if (membership.Project.Status != requestedStatus && !canChangeStatus)
        {
            return new UpdateProjectResult(UpdateProjectOutcome.Forbidden);
        }

        if (membership.Project.Version != request.Version)
        {
            return new UpdateProjectResult(UpdateProjectOutcome.VersionConflict);
        }

        membership.Project.Update(
            new ProjectDetailsChange
            {
                Name = request.Name.Trim(),
                Description = NormalizeDescription(request.Description),
                Status = requestedStatus,
            },
            new ChangeContext(accountId, timeProvider.GetUtcNow()));
        dbContext.ProjectAuditEvents.Add(new ProjectAuditEvent(
            Guid.NewGuid(),
            accountId,
            "project_updated",
            membership.Project.UpdatedAt,
            membership.Project.Id));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateProjectResult(UpdateProjectOutcome.VersionConflict);
        }

        return new UpdateProjectResult(
            UpdateProjectOutcome.Succeeded,
            ToResponse(
                membership.Project,
                membership.Roles.OrderBy(role => role.ProjectRole.SortOrder)
                    .Select(role => role.ProjectRole.Name)
                    .ToArray(),
                permissions));
    }

    public async Task<IReadOnlyList<ProjectRoleResponse>?> ListRolesAsync(
        Guid projectId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.ProjectReadPermission,
            cancellationToken))
        {
            return null;
        }

        return await dbContext.ProjectRoles
            .AsNoTracking()
            .Where(role => role.IsSystem && role.IsActive)
            .OrderBy(role => role.SortOrder)
            .Select(role => new ProjectRoleResponse(role.Code, role.Name, role.Description))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectMemberResponse>?> ListMembersAsync(
        Guid projectId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.ProjectReadPermission,
            cancellationToken))
        {
            return null;
        }

        var members = await dbContext.ProjectMembers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(member => member.Roles)
            .ThenInclude(mapping => mapping.ProjectRole)
            .Where(member => member.ProjectId == projectId && member.Status == ProjectMemberStatus.Active)
            .ToArrayAsync(cancellationToken);
        var accounts = await accountDirectory.GetByIdsAsync(
            members.Select(member => member.AccountId).ToArray(),
            cancellationToken);

        return members.OrderBy(member => member.JoinedAt)
            .Select(member => ToMemberResponse(
                member,
                accounts.GetValueOrDefault(member.AccountId)?.Username ?? member.AccountId.ToString()))
            .ToArray();
    }

    public async Task<ProjectMemberMutationResult> AddMemberAsync(
        Guid projectId,
        Guid actorAccountId,
        AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await GetMembershipWithPermissionsAsync(
            projectId,
            actorAccountId,
            cancellationToken);
        if (actor is null)
        {
            return new(ProjectMemberMutationOutcome.NotFound);
        }

        if (!HasPermission(actor, ProjectManagementConstants.ProjectMemberAddPermission) ||
            !HasPermission(actor, ProjectManagementConstants.ProjectRoleAssignPermission))
        {
            return new(ProjectMemberMutationOutcome.Forbidden);
        }

        var roles = await ResolveRolesAsync(request.RoleCodes, cancellationToken);
        if (roles is null)
        {
            return new(ProjectMemberMutationOutcome.InvalidRoles);
        }

        var account = await accountDirectory.FindActiveByUsernameAsync(
            request.Username,
            cancellationToken);
        if (account is null)
        {
            return new(ProjectMemberMutationOutcome.AccountNotFound);
        }

        var member = await dbContext.ProjectMembers
            .Include(item => item.Roles)
            .SingleOrDefaultAsync(
                item => item.ProjectId == projectId && item.AccountId == account.Id,
                cancellationToken);
        if (member?.Status == ProjectMemberStatus.Active)
        {
            return new(ProjectMemberMutationOutcome.AlreadyMember);
        }

        var now = timeProvider.GetUtcNow();
        if (member is null)
        {
            member = new ProjectMember(Guid.NewGuid(), projectId, account.Id, now, actorAccountId);
            dbContext.ProjectMembers.Add(member);
        }
        else
        {
            dbContext.ProjectMemberRoles.RemoveRange(member.Roles);
            member.Restore(actorAccountId, now);
        }

        AddRoleMappings(member, roles, actorAccountId, now);
        dbContext.ProjectAuditEvents.Add(new ProjectAuditEvent(
            Guid.NewGuid(),
            actorAccountId,
            "project_member_added",
            now,
            member.Id));

        await dbContext.SaveChangesAsync(cancellationToken);
        return new(
            ProjectMemberMutationOutcome.Succeeded,
            ToMemberResponse(member, account.Username, roles));
    }

    public async Task<ProjectMemberMutationResult> UpdateMemberRolesAsync(
        Guid projectId,
        Guid memberId,
        Guid actorAccountId,
        UpdateProjectMemberRolesRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await GetMembershipWithPermissionsAsync(
            projectId,
            actorAccountId,
            cancellationToken);
        if (actor is null)
        {
            return new(ProjectMemberMutationOutcome.NotFound);
        }

        if (!HasPermission(actor, ProjectManagementConstants.ProjectRoleAssignPermission))
        {
            return new(ProjectMemberMutationOutcome.Forbidden);
        }

        var roles = await ResolveRolesAsync(request.RoleCodes, cancellationToken);
        if (roles is null)
        {
            return new(ProjectMemberMutationOutcome.InvalidRoles);
        }

        var member = await dbContext.ProjectMembers
            .Include(item => item.Roles)
            .ThenInclude(mapping => mapping.ProjectRole)
            .SingleOrDefaultAsync(
                item => item.Id == memberId &&
                    item.ProjectId == projectId &&
                    item.Status == ProjectMemberStatus.Active,
                cancellationToken);
        if (member is null)
        {
            return new(ProjectMemberMutationOutcome.NotFound);
        }

        if (member.Version != request.Version)
        {
            return new(ProjectMemberMutationOutcome.VersionConflict);
        }

        var removesOwner = member.Roles.Any(mapping =>
                mapping.ProjectRole.Code == ProjectManagementConstants.OwnerRoleCode) &&
            roles.All(role => role.Code != ProjectManagementConstants.OwnerRoleCode);
        if (removesOwner && !await HasAnotherOwnerAsync(projectId, member.Id, cancellationToken))
        {
            return new(ProjectMemberMutationOutcome.LastOwner);
        }

        var now = timeProvider.GetUtcNow();
        dbContext.ProjectMemberRoles.RemoveRange(member.Roles);
        AddRoleMappings(member, roles, actorAccountId, now);
        member.RecordRoleChange(actorAccountId, now);
        dbContext.ProjectAuditEvents.Add(new ProjectAuditEvent(
            Guid.NewGuid(),
            actorAccountId,
            "project_member_roles_changed",
            now,
            member.Id));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(ProjectMemberMutationOutcome.VersionConflict);
        }

        var accounts = await accountDirectory.GetByIdsAsync([member.AccountId], cancellationToken);
        return new(
            ProjectMemberMutationOutcome.Succeeded,
            ToMemberResponse(
                member,
                accounts.GetValueOrDefault(member.AccountId)?.Username ?? member.AccountId.ToString(),
                roles));
    }

    public async Task<ProjectMemberMutationResult> RemoveMemberAsync(
        Guid projectId,
        Guid memberId,
        Guid actorAccountId,
        int version,
        CancellationToken cancellationToken)
    {
        var actor = await GetMembershipWithPermissionsAsync(
            projectId,
            actorAccountId,
            cancellationToken);
        if (actor is null)
        {
            return new(ProjectMemberMutationOutcome.NotFound);
        }

        if (!HasPermission(actor, ProjectManagementConstants.ProjectMemberRemovePermission))
        {
            return new(ProjectMemberMutationOutcome.Forbidden);
        }

        var member = await dbContext.ProjectMembers
            .Include(item => item.Roles)
            .ThenInclude(mapping => mapping.ProjectRole)
            .SingleOrDefaultAsync(
                item => item.Id == memberId &&
                    item.ProjectId == projectId &&
                    item.Status == ProjectMemberStatus.Active,
                cancellationToken);
        if (member is null)
        {
            return new(ProjectMemberMutationOutcome.NotFound);
        }

        if (member.Version != version)
        {
            return new(ProjectMemberMutationOutcome.VersionConflict);
        }

        if (member.Roles.Any(mapping =>
                mapping.ProjectRole.Code == ProjectManagementConstants.OwnerRoleCode) &&
            !await HasAnotherOwnerAsync(projectId, member.Id, cancellationToken))
        {
            return new(ProjectMemberMutationOutcome.LastOwner);
        }

        var now = timeProvider.GetUtcNow();
        member.Remove(actorAccountId, now);
        dbContext.ProjectAuditEvents.Add(new ProjectAuditEvent(
            Guid.NewGuid(),
            actorAccountId,
            "project_member_removed",
            now,
            member.Id));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(ProjectMemberMutationOutcome.VersionConflict);
        }

        return new(ProjectMemberMutationOutcome.Succeeded);
    }

    private async Task<ProjectMember?> GetMembershipWithPermissionsAsync(
        Guid projectId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers
            .AsSplitQuery()
            .Include(member => member.Roles)
            .ThenInclude(mapping => mapping.ProjectRole)
            .ThenInclude(role => role.Permissions)
            .ThenInclude(permission => permission.Permission)
            .SingleOrDefaultAsync(
                member => member.ProjectId == projectId &&
                    member.AccountId == accountId &&
                    member.Status == ProjectMemberStatus.Active,
                cancellationToken);
    }

    private async Task<bool> HasPermissionAsync(
        Guid projectId,
        Guid accountId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId &&
                member.AccountId == accountId &&
                member.Status == ProjectMemberStatus.Active &&
                member.Roles.Any(mapping => mapping.ProjectRole.Permissions.Any(permission =>
                    permission.Permission.Code == permissionCode)),
            cancellationToken);
    }

    private static bool HasPermission(ProjectMember member, string permissionCode)
    {
        return member.Roles.Any(mapping => mapping.ProjectRole.Permissions.Any(permission =>
            permission.Permission.Code == permissionCode));
    }

    private async Task<ProjectRole[]?> ResolveRolesAsync(
        IReadOnlyList<string> roleCodes,
        CancellationToken cancellationToken)
    {
        var normalizedCodes = roleCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();
        if (normalizedCodes.Length == 0)
        {
            return null;
        }

        var roles = await dbContext.ProjectRoles
            .Where(role => role.IsSystem && role.IsActive && normalizedCodes.Contains(role.Code))
            .OrderBy(role => role.SortOrder)
            .ToArrayAsync(cancellationToken);
        return roles.Length == normalizedCodes.Length ? roles : null;
    }

    private async Task<bool> HasAnotherOwnerAsync(
        Guid projectId,
        Guid excludedMemberId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId &&
                member.Id != excludedMemberId &&
                member.Status == ProjectMemberStatus.Active &&
                member.Roles.Any(mapping =>
                    mapping.ProjectRole.Code == ProjectManagementConstants.OwnerRoleCode),
            cancellationToken);
    }

    private void AddRoleMappings(
        ProjectMember member,
        IReadOnlyCollection<ProjectRole> roles,
        Guid actorAccountId,
        DateTimeOffset occurredAt)
    {
        foreach (var role in roles)
        {
            var mapping = new ProjectMemberRole(
                Guid.NewGuid(),
                member.Id,
                role.Id,
                occurredAt,
                actorAccountId);
            dbContext.ProjectMemberRoles.Add(mapping);
        }
    }

    private static ProjectMemberResponse ToMemberResponse(
        ProjectMember member,
        string username,
        IReadOnlyCollection<ProjectRole>? roles = null)
    {
        var roleCodes = roles?.Select(role => role.Code) ??
            member.Roles.OrderBy(mapping => mapping.ProjectRole.SortOrder)
                .Select(mapping => mapping.ProjectRole.Code);
        return new ProjectMemberResponse(
            member.Id,
            member.AccountId,
            username,
            member.Status.ToCode(),
            roleCodes.ToArray(),
            member.JoinedAt,
            member.Version);
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static ProjectResponse ToResponse(
        ProjectMember membership)
    {
        return ToResponse(
            membership.Project,
            membership.Roles.OrderBy(role => role.ProjectRole.SortOrder)
                .Select(role => role.ProjectRole.Name)
                .ToArray(),
            membership.Roles.SelectMany(role => role.ProjectRole.Permissions)
                .Select(permission => permission.Permission.Code)
                .Distinct()
                .Order()
                .ToArray());
    }

    private static ProjectResponse ToResponse(
        Project project,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions)
    {
        return new ProjectResponse(
            project.Id,
            project.Code,
            project.Name,
            project.Description,
            project.Status.ToCode(),
            roles,
            permissions,
            project.CreatedAt,
            project.UpdatedAt,
            project.Version);
    }
}
