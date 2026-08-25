using KhaiKang.CommonUtils.Models;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed class IssueService(
    ProjectManagementDbContext dbContext,
    IAccountDirectory accountDirectory,
    IResourceActorDirectory resourceActorDirectory,
    TimeProvider timeProvider)
{
    public async Task<PagedResult<IssueResponse>?> ListAsync(
        Guid projectId,
        Guid accountId,
        int page,
        int pageSize,
        IssueListQuery request,
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

        var query = dbContext.Issues
            .AsNoTracking()
            .Include(issue => issue.Project)
            .Include(issue => issue.IssueType)
            .Include(issue => issue.IssueStatus)
            .Include(issue => issue.IssuePriority)
            .Where(issue => issue.ProjectId == projectId);
        query = ApplyListFilters(query, request);
        var totalCount = await query.LongCountAsync(cancellationToken);
        var skip = (long)(page - 1) * pageSize;
        var issues = skip > int.MaxValue
            ? []
            : await ApplyListOrdering(
                query,
                request,
                string.Equals(
                    dbContext.Database.ProviderName,
                    "Microsoft.EntityFrameworkCore.Sqlite",
                    StringComparison.Ordinal))
                .Skip((int)skip)
                .Take(pageSize)
                .ToArrayAsync(cancellationToken);
        var accountIds = issues
            .SelectMany(issue => new Guid?[] { issue.ReporterAccountId, issue.AssigneeAccountId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var accounts = await accountDirectory.GetByIdsAsync(accountIds, cancellationToken);

        var items = issues.Select(issue => ToResponse(
            issue,
            issue.Project.Code,
            issue.IssueType,
            issue.IssueStatus,
            issue.IssuePriority,
            accounts)).ToArray();

        return new PagedResult<IssueResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    private static IQueryable<Issue> ApplyListFilters(
        IQueryable<Issue> query,
        IssueListQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            var normalizedSearch = search.ToLowerInvariant();
            var keyIssueNumber = TryGetIssueNumberFromKey(search);
            query = query.Where(issue =>
                issue.Title.ToLower().Contains(normalizedSearch) ||
                (keyIssueNumber.HasValue && issue.IssueNo == keyIssueNumber.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.TypeCode))
        {
            query = query.Where(issue => issue.IssueType.Code == request.TypeCode);
        }

        if (!string.IsNullOrWhiteSpace(request.StatusCode))
        {
            query = query.Where(issue => issue.IssueStatus.Code == request.StatusCode);
        }

        if (!string.IsNullOrWhiteSpace(request.PriorityCode))
        {
            query = query.Where(issue => issue.IssuePriority.Code == request.PriorityCode);
        }

        if (request.Unassigned == true)
        {
            query = query.Where(issue => issue.AssigneeAccountId == null);
        }
        else if (request.AssigneeAccountId.HasValue)
        {
            query = query.Where(issue => issue.AssigneeAccountId == request.AssigneeAccountId);
        }

        return query;
    }

    private static IOrderedQueryable<Issue> ApplyListOrdering(
        IQueryable<Issue> query,
        IssueListQuery request,
        bool useSqliteFallback)
    {
        var descending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        var sortByIssueNumber = string.Equals(request.SortBy, "issueNo", StringComparison.OrdinalIgnoreCase);
        if (sortByIssueNumber)
        {
            return descending
                ? query.OrderByDescending(issue => issue.IssueNo)
                : query.OrderBy(issue => issue.IssueNo);
        }

        if (useSqliteFallback)
        {
            return descending
                ? query.OrderByDescending(issue => issue.IssueNo)
                : query.OrderBy(issue => issue.IssueNo);
        }

        return descending
            ? query.OrderByDescending(issue => issue.UpdatedAt).ThenByDescending(issue => issue.IssueNo)
            : query.OrderBy(issue => issue.UpdatedAt).ThenBy(issue => issue.IssueNo);
    }

    private static int? TryGetIssueNumberFromKey(string value)
    {
        var separatorIndex = value.LastIndexOf('-');
        return separatorIndex < 1 || separatorIndex == value.Length - 1
            ? null
            : int.TryParse(value[(separatorIndex + 1)..], out var issueNo) && issueNo > 0
                ? issueNo
                : null;
    }

    public async Task<IssueMetadataResponse?> GetMetadataAsync(
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

        var types = await dbContext.IssueTypes.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .Select(item => new IssueOptionResponse
            {
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                Category = null,
            })
            .ToArrayAsync(cancellationToken);
        var statuses = await dbContext.IssueStatuses.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .Select(item => new IssueOptionResponse
            {
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                Category = item.Category.ToCode(),
            })
            .ToArrayAsync(cancellationToken);
        var priorities = await dbContext.IssuePriorities.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .Select(item => new IssueOptionResponse
            {
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                Category = null,
            })
            .ToArrayAsync(cancellationToken);

        return new IssueMetadataResponse
        {
            Types = types,
            Statuses = statuses,
            Priorities = priorities,
        };
    }

    public async Task<IssueResponse?> GetAsync(
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

        var issue = await dbContext.Issues.AsNoTracking()
            .Include(item => item.Project)
            .Include(item => item.IssueType)
            .Include(item => item.IssueStatus)
            .Include(item => item.IssuePriority)
            .SingleOrDefaultAsync(
                item => item.Id == issueId && item.ProjectId == projectId,
                cancellationToken);
        if (issue is null)
        {
            return null;
        }

        var accountIds = issue.AssigneeAccountId.HasValue
            ? new[] { issue.ReporterAccountId, issue.AssigneeAccountId.Value }
            : [issue.ReporterAccountId];
        var accounts = await accountDirectory.GetByIdsAsync(accountIds, cancellationToken);
        return ToResponse(
            issue,
            issue.Project.Code,
            issue.IssueType,
            issue.IssueStatus,
            issue.IssuePriority,
            accounts);
    }

    public async Task<IReadOnlyList<ResourceOperationResponse>?> ListOperationsAsync(
        Guid projectId,
        Guid issueId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
                projectId,
                accountId,
                ProjectManagementConstants.IssueReadPermission,
                cancellationToken) ||
            !await dbContext.Issues.AnyAsync(
                item => item.Id == issueId && item.ProjectId == projectId,
                cancellationToken))
        {
            return null;
        }

        var operations = await dbContext.ProjectAuditEvents.AsNoTracking()
            .Where(item => item.TargetId == issueId)
            .Select(item => new ResourceOperationResponse
            {
                Id = item.Id,
                EventType = item.EventType,
                ActorAccountId = item.ActorId,
                ActorType = item.ActorType,
                AiAccountName = item.ActorNameSnapshot,
                FixedRole = item.ActorFixedRoleSnapshot,
                OwnerAccountId = item.OwnerAccountIdSnapshot,
                OwnerDisplayName = item.OwnerDisplayNameSnapshot,
                OccurredAt = item.OccurredAt,
            })
            .ToArrayAsync(cancellationToken);
        return operations
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
            .ToArray();
    }

    public async Task<IssueMutationResult> CreateAsync(
        Guid projectId,
        Guid accountId,
        CreateIssueRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.IssueCreatePermission,
            cancellationToken))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        var actor = await resourceActorDirectory.FindActiveAsync(accountId, cancellationToken);
        var requestedType = request.TypeCode.Trim().ToLowerInvariant();
        if (actor is null ||
            actor.IsAi &&
            !((actor.FixedRole == "pm_ai" && requestedType is "story" or "task") ||
              (actor.FixedRole == "qa_ai" && requestedType == "bug")))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        if (actor.IsAi && actor.FixedRole == "qa_ai" &&
            request.AssigneeAccountId is { } requestedAssignee && requestedAssignee != actor.AccountId)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        var effectiveAssigneeAccountId = actor.IsAi && actor.FixedRole == "qa_ai"
            ? actor.AccountId
            : request.AssigneeAccountId;

        var project = await dbContext.Projects.SingleOrDefaultAsync(
            item => item.Id == projectId,
            cancellationToken);
        if (project is null)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.NotFound);
        }

        if (project.Status == ProjectStatus.Inactive)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.ProjectInactive);
        }

        if (request.AssigneeAccountId.HasValue &&
            !await HasPermissionAsync(
                projectId,
                accountId,
                ProjectManagementConstants.IssueAssigneeChangePermission,
                cancellationToken))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        var normalizedType = requestedType;
        var normalizedPriority = string.IsNullOrWhiteSpace(request.PriorityCode)
            ? "medium"
            : request.PriorityCode.Trim().ToLowerInvariant();
        var issueType = await dbContext.IssueTypes.SingleOrDefaultAsync(
            item => item.Code == normalizedType && item.IsActive,
            cancellationToken);
        var priority = await dbContext.IssuePriorities.SingleOrDefaultAsync(
            item => item.Code == normalizedPriority && item.IsActive,
            cancellationToken);
        var initialStatus = await dbContext.IssueStatuses.SingleAsync(
            item => item.Id == IssueCatalog.CreatedStatusId,
            cancellationToken);
        if (issueType is null || priority is null)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.InvalidOption);
        }

        if (effectiveAssigneeAccountId.HasValue &&
            !await IsAssignableMemberAsync(
                projectId,
                effectiveAssigneeAccountId.Value,
                cancellationToken))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.InvalidAssignee);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var issueNo = await NextIssueNoAsync(projectId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var context = ToChangeContext(actor!, now);
        var creation = new IssueCreation
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            IssueNo = issueNo,
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            UserStory = Normalize(request.UserStory),
            DefinitionOfDone = Normalize(request.DefinitionOfDone),
            IssueTypeId = issueType.Id,
            IssueStatusId = initialStatus.Id,
            IssuePriorityId = priority.Id,
            AssigneeAccountId = effectiveAssigneeAccountId,
        };
        var issue = Issue.Create(creation, context);
        dbContext.Issues.Add(issue);
        dbContext.ProjectAuditEvents.Add(ProjectAuditEvent.IssueCreated(issue.Id, context));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.NumberConflict);
        }

        var accountIds = effectiveAssigneeAccountId.HasValue
            ? new[] { accountId, effectiveAssigneeAccountId.Value }
            : [accountId];
        var accounts = await accountDirectory.GetByIdsAsync(accountIds, cancellationToken);
        return IssueMutationResult.Success(
            ToResponse(
                issue,
                project.Code,
                issueType,
                initialStatus,
                priority,
                accounts));
    }

    private async Task<int> NextIssueNoAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsNpgsql())
        {
            return (await dbContext.Issues
                .Where(issue => issue.ProjectId == projectId)
                .Select(issue => (int?)issue.IssueNo)
                .MaxAsync(cancellationToken) ?? 0) + 1;
        }

        const string counterType = "issue";
        return await dbContext.Database
            .SqlQuery<int>(
                $"SELECT public.next_project_number({counterType}, {projectId}) AS \"Value\"")
            .SingleAsync(cancellationToken);
    }

    public async Task<IssueMutationResult> ChangeStatusAsync(
        Guid projectId,
        Guid issueId,
        Guid accountId,
        UpdateIssueStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.IssueStatusChangePermission,
            cancellationToken))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        var issue = await dbContext.Issues
            .Include(item => item.Project)
            .Include(item => item.IssueType)
            .Include(item => item.IssueStatus)
            .Include(item => item.IssuePriority)
            .SingleOrDefaultAsync(
                item => item.Id == issueId && item.ProjectId == projectId,
                cancellationToken);
        if (issue is null)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.NotFound);
        }

        var actor = await resourceActorDirectory.FindActiveAsync(accountId, cancellationToken);
        if (!CanUpdateIssue(actor, issue))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        if (issue.Project.Status == ProjectStatus.Inactive)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.ProjectInactive);
        }

        if (issue.Version != request.Version)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.VersionConflict);
        }

        var normalizedStatus = request.StatusCode.Trim().ToLowerInvariant();
        var status = await dbContext.IssueStatuses.SingleOrDefaultAsync(
            item => item.Code == normalizedStatus && item.IsActive,
            cancellationToken);
        if (status is null)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.InvalidOption);
        }

        var now = timeProvider.GetUtcNow();
        var context = ToChangeContext(actor!, now);
        issue.ChangeStatus(
            status.Id,
            status.Category,
            context);
        dbContext.ProjectAuditEvents.Add(ProjectAuditEvent.IssueStatusChanged(issue.Id, context));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.VersionConflict);
        }

        var accountIds = issue.AssigneeAccountId.HasValue
            ? new[] { issue.ReporterAccountId, issue.AssigneeAccountId.Value }
            : [issue.ReporterAccountId];
        var accounts = await accountDirectory.GetByIdsAsync(accountIds, cancellationToken);
        return IssueMutationResult.Success(
            ToResponse(
                issue,
                issue.Project.Code,
                issue.IssueType,
                status,
                issue.IssuePriority,
                accounts));
    }

    public async Task<IssueMutationResult> UpdateAsync(
        Guid projectId,
        Guid issueId,
        Guid accountId,
        UpdateIssueRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.IssueUpdatePermission,
            cancellationToken))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        var issue = await dbContext.Issues
            .Include(item => item.Project)
            .Include(item => item.IssueType)
            .Include(item => item.IssueStatus)
            .Include(item => item.IssuePriority)
            .SingleOrDefaultAsync(
                item => item.Id == issueId && item.ProjectId == projectId,
                cancellationToken);
        if (issue is null)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.NotFound);
        }

        var actor = await resourceActorDirectory.FindActiveAsync(accountId, cancellationToken);
        if (!CanUpdateIssue(actor, issue))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        if (issue.Project.Status == ProjectStatus.Inactive)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.ProjectInactive);
        }

        if (issue.Version != request.Version)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.VersionConflict);
        }

        var normalizedType = request.TypeCode.Trim().ToLowerInvariant();
        var normalizedPriority = request.PriorityCode.Trim().ToLowerInvariant();
        if (!CanUpdateDetails(actor!, issue, request, normalizedType, normalizedPriority))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        var issueType = await dbContext.IssueTypes.SingleOrDefaultAsync(
            item => item.Code == normalizedType && item.IsActive,
            cancellationToken);
        var priority = await dbContext.IssuePriorities.SingleOrDefaultAsync(
            item => item.Code == normalizedPriority && item.IsActive,
            cancellationToken);
        if (issueType is null || priority is null)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.InvalidOption);
        }

        var now = timeProvider.GetUtcNow();
        var change = new IssueDetailsChange
        {
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            UserStory = Normalize(request.UserStory),
            DefinitionOfDone = Normalize(request.DefinitionOfDone),
            CompletionSummary = Normalize(request.CompletionSummary),
            IssueTypeId = issueType.Id,
            IssuePriorityId = priority.Id,
        };
        var context = ToChangeContext(actor!, now);
        issue.UpdateDetails(change, context);
        dbContext.ProjectAuditEvents.Add(ProjectAuditEvent.IssueUpdated(issue.Id, context));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.VersionConflict);
        }

        var accountIds = issue.AssigneeAccountId.HasValue
            ? new[] { issue.ReporterAccountId, issue.AssigneeAccountId.Value }
            : [issue.ReporterAccountId];
        var accounts = await accountDirectory.GetByIdsAsync(accountIds, cancellationToken);
        return IssueMutationResult.Success(
            ToResponse(
                issue,
                issue.Project.Code,
                issueType,
                issue.IssueStatus,
                priority,
                accounts));
    }

    public async Task<IssueMutationResult> ChangeAssigneeAsync(
        Guid projectId,
        Guid issueId,
        Guid accountId,
        UpdateIssueAssigneeRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
            projectId,
            accountId,
            ProjectManagementConstants.IssueAssigneeChangePermission,
            cancellationToken))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        var actor = await resourceActorDirectory.FindActiveAsync(accountId, cancellationToken);
        if (actor is null || actor.IsAi && actor.FixedRole != "pm_ai")
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);
        }

        var issue = await dbContext.Issues
            .Include(item => item.Project)
            .Include(item => item.IssueType)
            .Include(item => item.IssueStatus)
            .Include(item => item.IssuePriority)
            .SingleOrDefaultAsync(
                item => item.Id == issueId && item.ProjectId == projectId,
                cancellationToken);
        if (issue is null)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.NotFound);
        }

        if (issue.Project.Status == ProjectStatus.Inactive)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.ProjectInactive);
        }

        if (issue.Version != request.Version)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.VersionConflict);
        }

        if (request.AssigneeAccountId.HasValue &&
            !await IsAssignableMemberAsync(projectId, request.AssigneeAccountId.Value, cancellationToken))
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.InvalidAssignee);
        }

        var now = timeProvider.GetUtcNow();
        var context = ToChangeContext(actor, now);
        issue.ChangeAssignee(
            request.AssigneeAccountId,
            context);
        dbContext.ProjectAuditEvents.Add(ProjectAuditEvent.IssueAssigneeChanged(issue.Id, context));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return IssueMutationResult.Failure(IssueMutationOutcome.VersionConflict);
        }

        var accountIds = issue.AssigneeAccountId.HasValue
            ? new[] { issue.ReporterAccountId, issue.AssigneeAccountId.Value }
            : [issue.ReporterAccountId];
        var accounts = await accountDirectory.GetByIdsAsync(accountIds, cancellationToken);
        return IssueMutationResult.Success(
            ToResponse(
                issue,
                issue.Project.Code,
                issue.IssueType,
                issue.IssueStatus,
                issue.IssuePriority,
                accounts));
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

    private async Task<bool> IsAssignableMemberAsync(
        Guid projectId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var assignee = await resourceActorDirectory.FindActiveAsync(accountId, cancellationToken);
        if (assignee?.IsAi == true)
        {
            if (assignee.FixedRole is not ("development_ai" or "qa_ai"))
            {
                return false;
            }

            accountId = assignee.ScopeAccountId;
        }

        return await dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId &&
                member.AccountId == accountId &&
                member.Status == ProjectMemberStatus.Active,
            cancellationToken);
    }

    private static bool CanUpdateIssue(ResourceActorSnapshot? actor, Issue issue)
    {
        if (actor is null)
        {
            return false;
        }

        if (!actor.IsAi)
        {
            return true;
        }

        return actor.FixedRole switch
        {
            "pm_ai" => issue.IssueType.Code is "story" or "task",
            "development_ai" =>
                issue.IssueType.Code == "task" && issue.AssigneeAccountId == actor.AccountId,
            "qa_ai" =>
                issue.IssueType.Code is "task" or "bug" &&
                issue.AssigneeAccountId == actor.AccountId,
            _ => false,
        };
    }

    private static bool CanUpdateDetails(
        ResourceActorSnapshot actor,
        Issue issue,
        UpdateIssueRequest request,
        string normalizedType,
        string normalizedPriority)
    {
        if (!actor.IsAi || actor.FixedRole == "pm_ai" ||
            actor.FixedRole == "qa_ai" && issue.IssueType.Code == "bug")
        {
            return true;
        }

        return request.Title.Trim() == issue.Title &&
            Normalize(request.Description) == issue.Description &&
            Normalize(request.UserStory) == issue.UserStory &&
            Normalize(request.DefinitionOfDone) == issue.DefinitionOfDone &&
            normalizedType == issue.IssueType.Code &&
            normalizedPriority == issue.IssuePriority.Code;
    }

    private static IssueResponse ToResponse(
        Issue issue,
        string projectCode,
        IssueType issueType,
        IssueStatus issueStatus,
        IssuePriority issuePriority,
        IReadOnlyDictionary<Guid, AccountDirectoryEntry> accounts)
    {
        return new IssueResponse
        {
            Id = issue.Id,
            ProjectId = issue.ProjectId,
            IssueNo = issue.IssueNo,
            Key = $"{projectCode}-{issue.IssueNo}",
            Title = issue.Title,
            Description = issue.Description,
            UserStory = issue.UserStory,
            DefinitionOfDone = issue.DefinitionOfDone,
            CompletionSummary = issue.CompletionSummary,
            TypeCode = issueType.Code,
            TypeName = issueType.Name,
            StatusCode = issueStatus.Code,
            StatusName = issueStatus.Name,
            PriorityCode = issuePriority.Code,
            PriorityName = issuePriority.Name,
            ReporterAccountId = issue.ReporterAccountId,
            ReporterUsername = accounts.GetValueOrDefault(issue.ReporterAccountId)?.Username ??
                issue.ReporterAccountId.ToString(),
            AssigneeAccountId = issue.AssigneeAccountId,
            AssigneeUsername = issue.AssigneeAccountId.HasValue
                ? accounts.GetValueOrDefault(issue.AssigneeAccountId.Value)?.Username
                : null,
            CompletedAt = issue.CompletedAt,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt,
            Version = issue.Version,
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
