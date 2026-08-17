using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed class IssueRelationService(
    ProjectManagementDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<IssueRelationTypeResponse>?> ListTypesAsync(
        Guid projectId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.IssueReadPermission,
            cancellationToken))
        {
            return null;
        }

        return await dbContext.IssueRelationTypes
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .Select(item => new IssueRelationTypeResponse
            {
                Id = item.Id,
                Code = item.Code,
                ForwardLabel = item.ForwardLabel,
                ReverseLabel = item.ReverseLabel,
                DirectionKind = item.DirectionKind,
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IssueRelationResponse>?> ListAsync(
        Guid projectId,
        Guid issueId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.IssueReadPermission,
            cancellationToken))
        {
            return null;
        }

        if (!await dbContext.Issues.AnyAsync(
            item => item.Id == issueId && item.ProjectId == projectId,
            cancellationToken))
        {
            return null;
        }

        var relations = await RelationQuery()
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId &&
                !item.IsDeleted &&
                (item.SourceIssueId == issueId || item.TargetIssueId == issueId))
            .ToArrayAsync(cancellationToken);

        return relations
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Select(ToResponse)
            .ToArray();
    }

    public async Task<IssueRelationMutationResult> CreateAsync(
        Guid projectId,
        Guid issueId,
        Guid accountId,
        CreateIssueRelationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.IssueRelationCreatePermission,
            cancellationToken))
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.Forbidden);
        }

        var project = await dbContext.Projects.SingleOrDefaultAsync(
            item => item.Id == projectId,
            cancellationToken);
        if (project is null)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.NotFound);
        }

        if (project.Status == ProjectStatus.Inactive)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.ProjectInactive);
        }

        var relationTypeCode = request.RelationTypeCode.Trim().ToLowerInvariant();
        var relationType = await dbContext.IssueRelationTypes.SingleOrDefaultAsync(
            item => item.Code == relationTypeCode && item.IsActive,
            cancellationToken);
        if (relationType is null)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.InvalidType);
        }

        var direction = request.Direction.Trim().ToLowerInvariant();
        if (direction is not ("forward" or "reverse"))
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.InvalidDirection);
        }

        if (issueId == request.RelatedIssueId)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.SelfRelation);
        }

        var issues = await dbContext.Issues
            .Include(item => item.Project)
            .Include(item => item.IssueType)
            .Include(item => item.IssueStatus)
            .Where(item => item.ProjectId == projectId &&
                (item.Id == issueId || item.Id == request.RelatedIssueId))
            .ToArrayAsync(cancellationToken);
        if (issues.Length != 2)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.NotFound);
        }

        var sourceIssueId = direction == "forward" ? issueId : request.RelatedIssueId;
        var targetIssueId = direction == "forward" ? request.RelatedIssueId : issueId;
        if (relationType.DirectionKind == IssueRelationCatalog.Symmetric &&
            sourceIssueId.CompareTo(targetIssueId) > 0)
        {
            (sourceIssueId, targetIssueId) = (targetIssueId, sourceIssueId);
        }

        if (await dbContext.IssueRelations.AnyAsync(
            item => item.RelationTypeId == relationType.Id &&
                item.SourceIssueId == sourceIssueId &&
                item.TargetIssueId == targetIssueId &&
                !item.IsDeleted,
            cancellationToken))
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.Duplicate);
        }

        if (relationType.Id == IssueRelationCatalog.ParentOfId)
        {
            if (await dbContext.IssueRelations.AnyAsync(
                item => item.RelationTypeId == IssueRelationCatalog.ParentOfId &&
                    item.TargetIssueId == targetIssueId &&
                    !item.IsDeleted,
                cancellationToken))
            {
                return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.ParentConflict);
            }

            if (await CreatesHierarchyCycleAsync(
                projectId,
                sourceIssueId,
                targetIssueId,
                cancellationToken))
            {
                return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.HierarchyCycle);
            }
        }

        var now = timeProvider.GetUtcNow();
        var context = new ChangeContext(accountId, now);
        var relation = IssueRelation.Create(
            new IssueRelationCreation
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                RelationTypeId = relationType.Id,
                SourceIssueId = sourceIssueId,
                TargetIssueId = targetIssueId,
            },
            context);
        dbContext.IssueRelations.Add(relation);
        dbContext.ProjectAuditEvents.Add(ProjectAuditEvent.IssueRelationCreated(relation.Id, context));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.Duplicate);
        }

        relationType = await dbContext.IssueRelationTypes.AsNoTracking()
            .SingleAsync(item => item.Id == relation.RelationTypeId, cancellationToken);
        var source = issues.Single(item => item.Id == sourceIssueId);
        var target = issues.Single(item => item.Id == targetIssueId);
        return IssueRelationMutationResult.Created(
            ToResponse(relation, relationType, source, target));
    }

    public async Task<IssueRelationMutationResult> DeleteAsync(
        Guid projectId,
        Guid issueId,
        Guid relationId,
        Guid accountId,
        int version,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.IssueUpdatePermission,
            cancellationToken))
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.Forbidden);
        }

        var relation = await dbContext.IssueRelations
            .Include(item => item.Project)
            .SingleOrDefaultAsync(
                item => item.Id == relationId &&
                    item.ProjectId == projectId &&
                    !item.IsDeleted &&
                    (item.SourceIssueId == issueId || item.TargetIssueId == issueId),
                cancellationToken);
        if (relation is null)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.NotFound);
        }

        if (relation.Project.Status == ProjectStatus.Inactive)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.ProjectInactive);
        }

        if (relation.Version != version)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.VersionConflict);
        }

        var now = timeProvider.GetUtcNow();
        var context = new ChangeContext(accountId, now);
        relation.Delete(context);
        dbContext.ProjectAuditEvents.Add(ProjectAuditEvent.IssueRelationDeleted(relation.Id, context));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.VersionConflict);
        }

        return IssueRelationMutationResult.Deleted();
    }

    private async Task<bool> CreatesHierarchyCycleAsync(
        Guid projectId,
        Guid sourceIssueId,
        Guid targetIssueId,
        CancellationToken cancellationToken)
    {
        var edges = await dbContext.IssueRelations
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId &&
                item.RelationTypeId == IssueRelationCatalog.ParentOfId &&
                !item.IsDeleted)
            .Select(item => new { item.SourceIssueId, item.TargetIssueId })
            .ToArrayAsync(cancellationToken);
        var childrenByParent = edges
            .GroupBy(edge => edge.SourceIssueId)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.TargetIssueId).ToArray());
        var pending = new Queue<Guid>();
        var visited = new HashSet<Guid>();
        pending.Enqueue(targetIssueId);

        while (pending.TryDequeue(out var current))
        {
            if (!visited.Add(current))
            {
                continue;
            }

            if (current == sourceIssueId)
            {
                return true;
            }

            if (childrenByParent.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    pending.Enqueue(child);
                }
            }
        }

        return false;
    }

    private IQueryable<IssueRelation> RelationQuery()
    {
        return dbContext.IssueRelations
            .Include(item => item.RelationType)
            .Include(item => item.SourceIssue).ThenInclude(issue => issue.Project)
            .Include(item => item.SourceIssue).ThenInclude(issue => issue.IssueType)
            .Include(item => item.SourceIssue).ThenInclude(issue => issue.IssueStatus)
            .Include(item => item.TargetIssue).ThenInclude(issue => issue.Project)
            .Include(item => item.TargetIssue).ThenInclude(issue => issue.IssueType)
            .Include(item => item.TargetIssue).ThenInclude(issue => issue.IssueStatus);
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

    private static IssueRelationResponse ToResponse(IssueRelation relation)
    {
        return ToResponse(
            relation,
            relation.RelationType,
            relation.SourceIssue,
            relation.TargetIssue);
    }

    private static IssueRelationResponse ToResponse(
        IssueRelation relation,
        IssueRelationType relationType,
        Issue source,
        Issue target)
    {
        return new IssueRelationResponse
        {
            Id = relation.Id,
            ProjectId = relation.ProjectId,
            RelationTypeCode = relationType.Code,
            ForwardLabel = relationType.ForwardLabel,
            ReverseLabel = relationType.ReverseLabel,
            DirectionKind = relationType.DirectionKind,
            SourceIssue = ToIssueResponse(source),
            TargetIssue = ToIssueResponse(target),
            CreatedAt = relation.CreatedAt,
            Version = relation.Version,
        };
    }

    private static IssueRelationIssueResponse ToIssueResponse(Issue issue)
    {
        return new IssueRelationIssueResponse
        {
            Id = issue.Id,
            IssueNo = issue.IssueNo,
            Key = $"{issue.Project.Code}-{issue.IssueNo}",
            Title = issue.Title,
            TypeCode = issue.IssueType.Code,
            StatusCode = issue.IssueStatus.Code,
        };
    }
}
