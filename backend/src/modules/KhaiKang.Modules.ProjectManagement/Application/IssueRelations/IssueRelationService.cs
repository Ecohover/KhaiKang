using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed class IssueRelationService(
    ProjectManagementDbContext dbContext,
    IResourceActorDirectory resourceActorDirectory,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

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
        CreateIssueRelationCommand command,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            command.ProjectId,
            command.ActorAccountId,
            ProjectManagementConstants.IssueRelationCreatePermission,
            cancellationToken))
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.Forbidden);
        }

        var actor = await resourceActorDirectory.FindActiveAsync(
            command.ActorAccountId,
            cancellationToken);
        if (actor is null || actor.IsAi && actor.FixedRole != "pm_ai")
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.Forbidden);
        }

        var fingerprint = CreateFingerprint(command);
        await ReserveAsync(command, fingerprint, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await FindAndLockRecordAsync(command, cancellationToken);
        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(record.RequestFingerprint),
            Convert.FromHexString(fingerprint)))
        {
            await transaction.RollbackAsync(cancellationToken);
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.IdempotencyConflict);
        }

        if (record.State == IssueRelationIdempotencyState.TerminalSuccess)
        {
            var response = JsonSerializer.Deserialize<IssueRelationResponse>(
                record.ResponseSnapshot!,
                SnapshotJsonOptions)!;
            await transaction.CommitAsync(cancellationToken);
            return IssueRelationMutationResult.Created(response);
        }

        if (record.State == IssueRelationIdempotencyState.TerminalBusinessRejection)
        {
            var outcome = JsonSerializer.Deserialize<IssueRelationMutationOutcome>(
                record.RejectionSnapshot!,
                SnapshotJsonOptions);
            await transaction.CommitAsync(cancellationToken);
            return IssueRelationMutationResult.Failure(outcome);
        }

        var result = await CreateReservedAsync(
            command.ProjectId,
            command.IssueId,
            command.ActorAccountId,
            command.Request,
            cancellationToken);
        if (result.Outcome == IssueRelationMutationOutcome.Succeeded)
        {
            record.CompleteSuccess(
                JsonSerializer.Serialize(result.Relation, SnapshotJsonOptions),
                timeProvider.GetUtcNow());
        }
        else
        {
            record.CompleteBusinessRejection(
                JsonSerializer.Serialize(result.Outcome, SnapshotJsonOptions),
                timeProvider.GetUtcNow());
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
        }

        return result;
    }

    private async Task<IssueRelationMutationResult> CreateReservedAsync(
        Guid projectId,
        Guid issueId,
        Guid accountId,
        CreateIssueRelationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RelationTypeCode) || request.RelationTypeCode.Length > 50)
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.InvalidType);
        if (request.RelatedIssueId == Guid.Empty)
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.InvalidTarget);
        if (!string.Equals(request.Direction, "forward", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Direction, "reverse", StringComparison.OrdinalIgnoreCase))
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.InvalidDirection);

        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.IssueRelationCreatePermission,
            cancellationToken))
        {
            return IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.Forbidden);
        }

        var actor = await resourceActorDirectory.FindActiveAsync(accountId, cancellationToken);
        if (actor is null || actor.IsAi && actor.FixedRole != "pm_ai")
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
            var parentIssue = issues.Single(item => item.Id == sourceIssueId);
            var childIssue = issues.Single(item => item.Id == targetIssueId);
            if (parentIssue.IssueType.Code != "story" || childIssue.IssueType.Code != "task")
            {
                return IssueRelationMutationResult.Failure(
                    IssueRelationMutationOutcome.InvalidParentChildTypes);
            }

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
        var context = ToChangeContext(actor, now);
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

    private async Task ReserveAsync(
        CreateIssueRelationCommand command,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (await FindRecordQuery(command).AsNoTracking().AnyAsync(cancellationToken))
        {
            return;
        }

        var record = IssueRelationIdempotencyRecord.Reserve(new IssueRelationIdempotencyReservation
        {
            Id = Guid.NewGuid(),
            ActorAccountId = command.ActorAccountId,
            IdempotencyKey = command.IdempotencyKey,
            RequestFingerprint = fingerprint,
            CreatedAt = timeProvider.GetUtcNow(),
        });
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.IssueRelationIdempotencyRecords.Add(record);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    private async Task<IssueRelationIdempotencyRecord> FindAndLockRecordAsync(
        CreateIssueRelationCommand command,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsNpgsql())
        {
            return await dbContext.IssueRelationIdempotencyRecords.FromSqlInterpolated($"""
                SELECT * FROM issue_relation_idempotency_records
                WHERE actor_account_id = {command.ActorAccountId}
                  AND idempotency_key = {command.IdempotencyKey}
                FOR UPDATE
                """).SingleAsync(cancellationToken);
        }

        return await FindRecordQuery(command).SingleAsync(cancellationToken);
    }

    private IQueryable<IssueRelationIdempotencyRecord> FindRecordQuery(
        CreateIssueRelationCommand command) =>
        dbContext.IssueRelationIdempotencyRecords.Where(record =>
            record.ActorAccountId == command.ActorAccountId &&
            record.IdempotencyKey == command.IdempotencyKey);

    private static string CreateFingerprint(CreateIssueRelationCommand command)
    {
        var request = command.Request;
        var canonical = JsonSerializer.Serialize(new
        {
            projectId = command.ProjectId.ToString("D"),
            issueId = command.IssueId.ToString("D"),
            relationTypeCode = request.RelationTypeCode?.Trim().ToLowerInvariant(),
            relatedIssueId = request.RelatedIssueId.ToString("D"),
            direction = request.Direction?.Trim().ToLowerInvariant(),
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

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

        var actor = await resourceActorDirectory.FindActiveAsync(accountId, cancellationToken);
        if (actor is null || actor.IsAi && actor.FixedRole != "pm_ai")
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
        var context = ToChangeContext(actor, now);
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

    private static ChangeContext ToChangeContext(ResourceActorSnapshot actor, DateTimeOffset occurredAt) =>
        new(
            actor.AccountId,
            occurredAt,
            new OperationActorSnapshot
            {
                ActorType = actor.IsAi ? "ai_agent" : "human",
                ActorName = actor.IsAi ? actor.AccountName : null,
                ActorFixedRole = actor.FixedRole,
                OwnerAccountId = actor.OwnerAccountId,
                OwnerDisplayName = actor.OwnerDisplayName,
            });

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
        var actor = await resourceActorDirectory.FindActiveAsync(accountId, cancellationToken);
        if (actor is null)
        {
            return false;
        }

        return await dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId &&
                member.AccountId == actor.ScopeAccountId &&
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
