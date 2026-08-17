using System.Text.RegularExpressions;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.TestManagement.Contracts;
using KhaiKang.Modules.TestManagement.Domain;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KhaiKang.Modules.TestManagement.Application;

public sealed class TestManagementService(
    TestManagementDbContext dbContext,
    IAccountDirectory accountDirectory,
    IProjectDirectory projectDirectory,
    IIssueDirectory issueDirectory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<TestTagResponse>> ListTagsAsync(CancellationToken cancellationToken) =>
        await dbContext.Tags.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new TestTagResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Status = x.Status.ToCode(),
                Version = x.Version,
            })
            .ToListAsync(cancellationToken);

    public async Task<TestManagementResult<TestTagResponse>> CreateTagAsync(
        Guid accountId, CreateTestTagRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await dbContext.Tags.AnyAsync(x => x.Name.ToUpper() == name.ToUpper(), cancellationToken))
            return TestManagementResult<TestTagResponse>.Failure(
                TestManagementOutcome.Conflict,
                "test_tag_name_conflict");
        var tag = new TestTag(Guid.NewGuid(), name, Clean(request.Description), accountId, timeProvider.GetUtcNow());
        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TestManagementResult<TestTagResponse>.Success(ToTagResponse(tag));
    }

    public async Task<TestManagementResult<TestTagResponse>> UpdateTagAsync(
        Guid tagId, Guid accountId, UpdateTestTagRequest request, CancellationToken cancellationToken)
    {
        var tag = await dbContext.Tags.SingleOrDefaultAsync(x => x.Id == tagId, cancellationToken);
        if (tag is null) return TestManagementResult<TestTagResponse>.Failure(TestManagementOutcome.NotFound);
        if (tag.Version != request.Version)
        {
            return TestManagementResult<TestTagResponse>.Failure(
                TestManagementOutcome.Conflict,
                "test_tag_version_conflict");
        }
        var name = request.Name.Trim();
        if (await dbContext.Tags.AnyAsync(x => x.Id != tagId && x.Name.ToUpper() == name.ToUpper(), cancellationToken))
            return TestManagementResult<TestTagResponse>.Failure(
                TestManagementOutcome.Conflict,
                "test_tag_name_conflict");
        if (!TestManagementCodes.TryParseAssetStatus(request.Status, out var status))
        {
            return TestManagementResult<TestTagResponse>.Failure(
                TestManagementOutcome.Invalid,
                "test_tag_status_invalid");
        }

        tag.Update(name, Clean(request.Description), status, accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return TestManagementResult<TestTagResponse>.Success(ToTagResponse(tag));
    }

    public async Task<IReadOnlyList<TestWorkspaceResponse>> ListWorkspacesAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Members.AsNoTracking()
            .Where(x => x.AccountId == accountId &&
                x.Status == TestWorkspaceMemberStatus.Active)
            .OrderBy(x => x.Workspace.Name)
            .Select(x => ToWorkspaceResponse(x.Workspace, x.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<TestWorkspaceResponse?> GetWorkspaceAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Members.AsNoTracking()
            .Where(x => x.TestWorkspaceId == workspaceId &&
                x.AccountId == accountId &&
                x.Status == TestWorkspaceMemberStatus.Active)
            .Select(x => ToWorkspaceResponse(x.Workspace, x.Role))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TestManagementResult<IReadOnlyList<TestWorkspaceProjectResponse>>> ListWorkspaceProjectsAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<IReadOnlyList<TestWorkspaceProjectResponse>>.Failure(
                TestManagementOutcome.NotFound);
        }

        var links = await dbContext.WorkspaceProjects
            .AsNoTracking()
            .Where(link => link.TestWorkspaceId == workspaceId)
            .ToArrayAsync(cancellationToken);
        var projects = await projectDirectory.GetByIdsAsync(
            links.Select(link => link.ProjectId).ToArray(), cancellationToken);
        var response = links
            .Where(link => projects.ContainsKey(link.ProjectId))
            .Select(link => ToWorkspaceProjectResponse(link, projects[link.ProjectId]))
            .OrderBy(link => link.Name)
            .ThenBy(link => link.Code)
            .ToArray();
        return TestManagementResult<IReadOnlyList<TestWorkspaceProjectResponse>>.Success(response);
    }

    public async Task<TestManagementResult<TestWorkspaceProjectResponse>> LinkWorkspaceProjectAsync(
        Guid workspaceId,
        Guid accountId,
        LinkTestWorkspaceProjectRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestWorkspaceProjectResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (!CanManage(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestWorkspaceProjectResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var project = await projectDirectory.FindAccessibleAsync(
            request.ProjectId, accountId, cancellationToken);
        if (project is null)
        {
            return TestManagementResult<TestWorkspaceProjectResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (project.Status != ProjectStatus.Active)
        {
            return TestManagementResult<TestWorkspaceProjectResponse>.Failure(
                TestManagementOutcome.Conflict,
                "project_not_active");
        }

        if (await dbContext.WorkspaceProjects.AnyAsync(
            link => link.TestWorkspaceId == workspaceId && link.ProjectId == request.ProjectId,
            cancellationToken))
        {
            return TestManagementResult<TestWorkspaceProjectResponse>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_project_already_linked");
        }

        var link = new TestWorkspaceProject(
            Guid.NewGuid(), workspaceId, request.ProjectId, accountId, timeProvider.GetUtcNow());
        dbContext.WorkspaceProjects.Add(link);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                ConstraintName: "uq_test_workspace_projects_workspace_project",
            })
        {
            return TestManagementResult<TestWorkspaceProjectResponse>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_project_already_linked");
        }

        return TestManagementResult<TestWorkspaceProjectResponse>.Success(
            ToWorkspaceProjectResponse(link, project));
    }

    public async Task<TestManagementResult<object>> UnlinkWorkspaceProjectAsync(
        Guid workspaceId,
        Guid projectId,
        Guid accountId,
        int version,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<object>.Failure(TestManagementOutcome.NotFound);
        }

        if (!CanManage(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<object>.Failure(TestManagementOutcome.Forbidden);
        }

        var link = await dbContext.WorkspaceProjects.SingleOrDefaultAsync(
            item => item.TestWorkspaceId == workspaceId && item.ProjectId == projectId,
            cancellationToken);
        if (link is null)
        {
            return TestManagementResult<object>.Failure(TestManagementOutcome.NotFound);
        }

        if (link.Version != version)
        {
            return TestManagementResult<object>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_project_version_conflict");
        }

        var hasTraceDependencies = await dbContext.CaseRequirementLinks.AnyAsync(
                item => item.TestWorkspaceId == workspaceId &&
                    item.ProjectId == projectId &&
                    !item.IsDeleted,
                cancellationToken) ||
            await dbContext.Plans.AnyAsync(
                item => item.TestWorkspaceId == workspaceId &&
                    item.TestIssueProjectId == projectId,
                cancellationToken) ||
            await dbContext.Runs.AnyAsync(
                item => item.TestIssueProjectId == projectId &&
                    dbContext.Plans.Any(plan =>
                        plan.Id == item.TestPlanId && plan.TestWorkspaceId == workspaceId),
                cancellationToken) ||
            await dbContext.RunBugLinks.AnyAsync(
                item => item.TestWorkspaceId == workspaceId && item.ProjectId == projectId,
                cancellationToken);
        if (hasTraceDependencies)
        {
            return TestManagementResult<object>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_project_has_trace_links");
        }

        dbContext.WorkspaceProjects.Remove(link);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TestManagementResult<object>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_project_version_conflict");
        }

        return TestManagementResult<object>.Success(new object());
    }

    public async Task<TestManagementResult<TestWorkspaceResponse>> CreateWorkspaceAsync(
        Guid accountId,
        CreateTestWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await dbContext.Workspaces.AnyAsync(x => x.Name == name, cancellationToken))
        {
            return TestManagementResult<TestWorkspaceResponse>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_name_conflict");
        }

        var prefix = string.IsNullOrWhiteSpace(request.Prefix)
            ? await GeneratePrefixAsync(name, cancellationToken)
            : request.Prefix.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(prefix, "^[A-Z][A-Z0-9]{1,9}$"))
        {
            return TestManagementResult<TestWorkspaceResponse>.Failure(
                TestManagementOutcome.Invalid,
                "workspace_prefix_invalid");
        }
        if (await dbContext.Workspaces.AnyAsync(x => x.Prefix == prefix, cancellationToken))
        {
            return TestManagementResult<TestWorkspaceResponse>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_prefix_conflict");
        }

        var now = timeProvider.GetUtcNow();
        var workspace = new TestWorkspace(
            Guid.NewGuid(), name, prefix, Clean(request.Description), accountId, now);
        var owner = new TestWorkspaceMember(
            Guid.NewGuid(), workspace.Id, accountId, TestWorkspaceRole.Owner, accountId, now);
        dbContext.AddRange(workspace, owner);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                ConstraintName: "uq_test_workspaces_prefix",
            })
        {
            return TestManagementResult<TestWorkspaceResponse>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_prefix_conflict");
        }
        return TestManagementResult<TestWorkspaceResponse>.Success(
            ToWorkspaceResponse(workspace, TestWorkspaceRole.Owner));
    }

    public async Task<TestManagementResult<TestWorkspaceResponse>> UpdateWorkspaceAsync(
        Guid workspaceId,
        Guid accountId,
        UpdateTestWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestWorkspaceResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (access.Role != TestWorkspaceRole.Owner)
        {
            return TestManagementResult<TestWorkspaceResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var workspace = access.Workspace;
        if (workspace.Version != request.Version)
        {
            return TestManagementResult<TestWorkspaceResponse>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_version_conflict");
        }

        var name = request.Name.Trim();
        if (await dbContext.Workspaces.AnyAsync(
            x => x.Id != workspaceId && x.Name == name, cancellationToken))
        {
            return TestManagementResult<TestWorkspaceResponse>.Failure(
                TestManagementOutcome.Conflict,
                "workspace_name_conflict");
        }

        if (!TestManagementCodes.TryParseAssetStatus(request.Status, out var status))
        {
            return TestManagementResult<TestWorkspaceResponse>.Failure(
                TestManagementOutcome.Invalid,
                "workspace_status_invalid");
        }

        workspace.Update(name, Clean(request.Description), status, accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return TestManagementResult<TestWorkspaceResponse>.Success(
            ToWorkspaceResponse(workspace, access.Role));
    }

    public async Task<TestManagementResult<IReadOnlyList<TestWorkspaceMemberResponse>>> ListMembersAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<IReadOnlyList<TestWorkspaceMemberResponse>>.Failure(
                TestManagementOutcome.NotFound);
        }

        var members = await dbContext.Members.AsNoTracking()
            .Where(x => x.TestWorkspaceId == workspaceId &&
                x.Status == TestWorkspaceMemberStatus.Active)
            .OrderBy(x => x.JoinedAt).ToListAsync(cancellationToken);
        var accounts = await accountDirectory.GetByIdsAsync(
            members.Select(x => x.AccountId).ToArray(), cancellationToken);
        return TestManagementResult<IReadOnlyList<TestWorkspaceMemberResponse>>.Success(members.Select(x =>
            new TestWorkspaceMemberResponse
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Username = accounts.GetValueOrDefault(x.AccountId)?.Username ?? x.AccountId.ToString(),
                Role = x.Role.ToCode(),
                Status = x.Status.ToCode(),
                JoinedAt = x.JoinedAt,
                Version = x.Version,
            }).ToArray());
    }

    public async Task<TestManagementResult<TestWorkspaceMemberResponse>> AddMemberAsync(
        Guid workspaceId,
        Guid accountId,
        AddTestWorkspaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(TestManagementOutcome.NotFound);
        }

        var hasValidRole = TestManagementCodes.TryParseWorkspaceRole(request.Role, out var role);
        if (!CanManage(access.Role) || !hasValidRole)
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(
                access.Role is TestWorkspaceRole.Owner or TestWorkspaceRole.Manager
                    ? TestManagementOutcome.Invalid
                    : TestManagementOutcome.Forbidden);
        }

        var account = await accountDirectory.FindActiveByUsernameAsync(
            request.Username.Trim(), cancellationToken);
        if (account is null)
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(
                TestManagementOutcome.NotFound,
                "account_not_found");
        }

        var member = await dbContext.Members.SingleOrDefaultAsync(
            x => x.TestWorkspaceId == workspaceId && x.AccountId == account.Id,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (member?.Status == TestWorkspaceMemberStatus.Active)
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(
                TestManagementOutcome.Conflict,
                "member_already_active");
        }

        if (member is null)
        {
            member = new TestWorkspaceMember(
                Guid.NewGuid(), workspaceId, account.Id, role, accountId, now);
            dbContext.Members.Add(member);
        }
        else
        {
            member.Restore(role, accountId, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return TestManagementResult<TestWorkspaceMemberResponse>.Success(new TestWorkspaceMemberResponse
        {
            Id = member.Id,
            AccountId = member.AccountId,
            Username = account.Username,
            Role = member.Role.ToCode(),
            Status = member.Status.ToCode(),
            JoinedAt = member.JoinedAt,
            Version = member.Version,
        });
    }

    public async Task<TestManagementResult<TestWorkspaceMemberResponse>> UpdateMemberAsync(
        Guid workspaceId,
        Guid memberId,
        Guid accountId,
        UpdateTestWorkspaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (!CanManage(access.Role))
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var member = await dbContext.Members.SingleOrDefaultAsync(
            x => x.Id == memberId && x.TestWorkspaceId == workspaceId &&
                x.Status == TestWorkspaceMemberStatus.Active,
            cancellationToken);
        if (member is null)
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (!TestManagementCodes.TryParseWorkspaceRole(request.Role, out var role))
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(TestManagementOutcome.Invalid);
        }

        if (member.Version != request.Version)
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(
                TestManagementOutcome.Conflict,
                "member_version_conflict");
        }

        if (member.Role == TestWorkspaceRole.Owner && role != TestWorkspaceRole.Owner &&
            await ActiveOwnerCountAsync(workspaceId, cancellationToken) <= 1)
        {
            return TestManagementResult<TestWorkspaceMemberResponse>.Failure(
                TestManagementOutcome.Conflict,
                "last_owner");
        }

        member.ChangeRole(role, accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        var accounts = await accountDirectory.GetByIdsAsync([member.AccountId], cancellationToken);
        return TestManagementResult<TestWorkspaceMemberResponse>.Success(new TestWorkspaceMemberResponse
        {
            Id = member.Id,
            AccountId = member.AccountId,
            Username = accounts.GetValueOrDefault(member.AccountId)?.Username ?? member.AccountId.ToString(),
            Role = member.Role.ToCode(),
            Status = member.Status.ToCode(),
            JoinedAt = member.JoinedAt,
            Version = member.Version,
        });
    }

    public async Task<TestManagementResult<object>> RemoveMemberAsync(
        Guid workspaceId,
        Guid memberId,
        Guid accountId,
        int version,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<object>.Failure(TestManagementOutcome.NotFound);
        }

        if (!CanManage(access.Role))
        {
            return TestManagementResult<object>.Failure(TestManagementOutcome.Forbidden);
        }

        var member = await dbContext.Members.SingleOrDefaultAsync(
            x => x.Id == memberId && x.TestWorkspaceId == workspaceId &&
                x.Status == TestWorkspaceMemberStatus.Active,
            cancellationToken);
        if (member is null)
        {
            return TestManagementResult<object>.Failure(TestManagementOutcome.NotFound);
        }

        if (member.Version != version)
        {
            return TestManagementResult<object>.Failure(
                TestManagementOutcome.Conflict,
                "member_version_conflict");
        }

        if (member.Role == TestWorkspaceRole.Owner &&
            await ActiveOwnerCountAsync(workspaceId, cancellationToken) <= 1)
        {
            return TestManagementResult<object>.Failure(
                TestManagementOutcome.Conflict,
                "last_owner");
        }

        member.Remove(accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return TestManagementResult<object>.Success(new object());
    }

    public async Task<TestManagementResult<IReadOnlyList<TestSuiteResponse>>> ListSuitesAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<IReadOnlyList<TestSuiteResponse>>.Failure(
                TestManagementOutcome.NotFound);
        }

        var suites = await dbContext.Suites.AsNoTracking()
            .Where(x => x.TestWorkspaceId == workspaceId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return TestManagementResult<IReadOnlyList<TestSuiteResponse>>.Success(MapSuites(suites));
    }

    public async Task<TestManagementResult<TestSuiteResponse>> CreateSuiteAsync(
        Guid workspaceId,
        Guid accountId,
        CreateTestSuiteRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestSuiteResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (!CanManageAssets(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestSuiteResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var depth = await ParentDepthAsync(workspaceId, request.ParentId, cancellationToken);
        if (depth is null || depth >= 5)
        {
            return TestManagementResult<TestSuiteResponse>.Failure(
                TestManagementOutcome.Invalid,
                "invalid_parent");
        }

        if (await HasSuiteNameAsync(workspaceId, request.ParentId, request.Name, null, cancellationToken))
        {
            return TestManagementResult<TestSuiteResponse>.Failure(
                TestManagementOutcome.Conflict,
                "suite_name_conflict");
        }

        var suite = new TestSuite(
            Guid.NewGuid(), workspaceId, request.ParentId, request.Name.Trim(),
            Clean(request.Description), request.SortOrder, accountId, timeProvider.GetUtcNow());
        dbContext.Suites.Add(suite);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TestManagementResult<TestSuiteResponse>.Success(
            ToSuiteResponse(suite, depth.Value + 1));
    }

    public async Task<TestManagementResult<TestSuiteResponse>> UpdateSuiteAsync(
        Guid workspaceId,
        Guid suiteId,
        Guid accountId,
        UpdateTestSuiteRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestSuiteResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (!CanManageAssets(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestSuiteResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var suite = await dbContext.Suites.SingleOrDefaultAsync(
            x => x.Id == suiteId && x.TestWorkspaceId == workspaceId, cancellationToken);
        if (suite is null)
        {
            return TestManagementResult<TestSuiteResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (suite.Version != request.Version)
        {
            return TestManagementResult<TestSuiteResponse>.Failure(
                TestManagementOutcome.Conflict,
                "suite_version_conflict");
        }

        if (request.ParentId == suiteId)
        {
            return TestManagementResult<TestSuiteResponse>.Failure(
                TestManagementOutcome.Invalid,
                "invalid_parent");
        }

        var allSuites = await dbContext.Suites.AsNoTracking()
            .Where(x => x.TestWorkspaceId == workspaceId).ToListAsync(cancellationToken);
        if (DescendantIds(suiteId, allSuites).Contains(request.ParentId ?? Guid.Empty))
        {
            return TestManagementResult<TestSuiteResponse>.Failure(
                TestManagementOutcome.Invalid,
                "suite_cycle");
        }

        var parentDepth = DepthOf(request.ParentId, allSuites);
        var subtreeHeight = HeightOf(suiteId, allSuites);
        if (parentDepth is null || parentDepth.Value + subtreeHeight > 5)
        {
            return TestManagementResult<TestSuiteResponse>.Failure(
                TestManagementOutcome.Invalid,
                "suite_depth");
        }

        if (await HasSuiteNameAsync(
            workspaceId, request.ParentId, request.Name, suiteId, cancellationToken))
        {
            return TestManagementResult<TestSuiteResponse>.Failure(
                TestManagementOutcome.Conflict,
                "suite_name_conflict");
        }

        if (!TestManagementCodes.TryParseAssetStatus(request.Status, out var status))
        {
            return TestManagementResult<TestSuiteResponse>.Failure(
                TestManagementOutcome.Invalid,
                "test_suite_status_invalid");
        }

        suite.Update(
            request.ParentId, request.Name.Trim(), Clean(request.Description),
            request.SortOrder, status, accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return TestManagementResult<TestSuiteResponse>.Success(
            ToSuiteResponse(suite, parentDepth.Value + 1));
    }

    public async Task<TestManagementResult<IReadOnlyList<TestCaseResponse>>> ListCasesAsync(
        Guid workspaceId,
        Guid accountId,
        Guid? suiteId,
        string? search,
        string? status,
        Guid? tagId,
        CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<IReadOnlyList<TestCaseResponse>>.Failure(
                TestManagementOutcome.NotFound);
        }

        var query = dbContext.Cases.AsNoTracking()
            .Where(x => x.Suite.TestWorkspaceId == workspaceId);
        if (suiteId is not null)
        {
            query = query.Where(x => x.TestSuiteId == suiteId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Title.Contains(term));
        }
        if (TestManagementCodes.TryParseAssetStatus(status, out var caseStatus))
        {
            query = query.Where(x => x.Status == caseStatus);
        }
        if (tagId is not null)
        {
            query = query.Where(x => x.Tags.Any(tag => tag.TestTagId == tagId));
        }

        var cases = await query
            .Include(x => x.Steps)
            .Include(x => x.Tags).ThenInclude(x => x.Tag)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);
        return TestManagementResult<IReadOnlyList<TestCaseResponse>>.Success(
            cases.Select(ToCaseResponse).ToArray());
    }

    public async Task<TestManagementResult<TestCaseResponse>> CreateCaseAsync(
        Guid workspaceId,
        Guid accountId,
        CreateTestCaseRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestCaseResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (!CanManageAssets(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestCaseResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var suite = await dbContext.Suites.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == request.SuiteId && x.TestWorkspaceId == workspaceId,
            cancellationToken);
        if (suite is null || suite.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestCaseResponse>.Failure(
                TestManagementOutcome.NotFound,
                "test_suite_not_found");
        }

        var tags = await ActiveTagsAsync(request.TagIds, cancellationToken);
        if (tags is null)
        {
            return TestManagementResult<TestCaseResponse>.Failure(
                TestManagementOutcome.Invalid,
                "test_tag_not_found");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var testCase = new TestCase(
            Guid.NewGuid(),
            workspaceId,
            suite.Id,
            await NextNumberAsync(TestNumberType.Case, workspaceId, cancellationToken),
            request.Title.Trim(),
            Clean(request.Description),
            Clean(request.Preconditions),
            Clean(request.OverallExpectedResult),
            request.SortOrder,
            accountId,
            now);
        for (var index = 0; index < request.Steps.Count; index++)
        {
            testCase.AddStep(new TestStep(
                Guid.NewGuid(),
                testCase.Id,
                index + 1,
                request.Steps[index].Action.Trim(),
                request.Steps[index].ExpectedResult.Trim(),
                accountId,
                now));
        }

        dbContext.Cases.Add(testCase);
        foreach (var tag in tags)
            dbContext.CaseTags.Add(new TestCaseTag(Guid.NewGuid(), testCase.Id, tag.Id, accountId, now));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TestManagementResult<TestCaseResponse>.Failure(
                TestManagementOutcome.Conflict,
                "case_number_conflict");
        }
        await dbContext.Entry(testCase).Collection(x => x.Tags).Query().Include(x => x.Tag).LoadAsync(cancellationToken);
        return TestManagementResult<TestCaseResponse>.Success(ToCaseResponse(testCase));
    }

    public async Task<TestManagementResult<TestCaseResponse>> GetCaseAsync(
        Guid workspaceId,
        Guid caseId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<TestCaseResponse>.Failure(TestManagementOutcome.NotFound);
        }

        var testCase = await dbContext.Cases.AsNoTracking()
            .Include(x => x.Steps)
            .Include(x => x.Tags).ThenInclude(x => x.Tag)
            .SingleOrDefaultAsync(x => x.Id == caseId && x.Suite.TestWorkspaceId == workspaceId, cancellationToken);
        return testCase is null
            ? TestManagementResult<TestCaseResponse>.Failure(TestManagementOutcome.NotFound)
            : TestManagementResult<TestCaseResponse>.Success(ToCaseResponse(testCase));
    }

    public async Task<TestManagementResult<TestCaseResponse>> UpdateCaseAsync(
        Guid workspaceId,
        Guid caseId,
        Guid accountId,
        UpdateTestCaseRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestCaseResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (!CanManageAssets(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestCaseResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var testCase = await dbContext.Cases
            .Include(x => x.Steps)
            .Include(x => x.Tags).ThenInclude(x => x.Tag)
            .SingleOrDefaultAsync(x => x.Id == caseId && x.Suite.TestWorkspaceId == workspaceId, cancellationToken);
        if (testCase is null)
        {
            return TestManagementResult<TestCaseResponse>.Failure(TestManagementOutcome.NotFound);
        }

        if (testCase.Version != request.Version)
        {
            return TestManagementResult<TestCaseResponse>.Failure(
                TestManagementOutcome.Conflict,
                "case_version_conflict");
        }

        var targetSuite = await dbContext.Suites.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == request.SuiteId && x.TestWorkspaceId == workspaceId,
            cancellationToken);
        if (targetSuite is null || targetSuite.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestCaseResponse>.Failure(
                TestManagementOutcome.Conflict,
                "test_suite_not_found");
        }

        IReadOnlyList<TestTag>? tags = null;
        if (request.TagIds is not null)
        {
            tags = await ActiveTagsAsync(request.TagIds, cancellationToken);
            if (tags is null)
            {
                return TestManagementResult<TestCaseResponse>.Failure(
                    TestManagementOutcome.Invalid,
                    "test_tag_not_found");
            }
        }

        if (!TestManagementCodes.TryParseAssetStatus(request.Status, out var status))
        {
            return TestManagementResult<TestCaseResponse>.Failure(
                TestManagementOutcome.Invalid,
                "test_case_status_invalid");
        }

        var now = timeProvider.GetUtcNow();
        testCase.Update(
            targetSuite.Id,
            request.Title.Trim(),
            Clean(request.Description),
            Clean(request.Preconditions),
            Clean(request.OverallExpectedResult),
            request.SortOrder,
            status,
            accountId,
            now);

        foreach (var step in testCase.Steps.ToList())
        {
            dbContext.CaseSteps.Remove(step);
        }
        testCase.ClearSteps();
        if (tags is not null)
        {
            dbContext.CaseTags.RemoveRange(testCase.Tags);
            foreach (var tag in tags)
                dbContext.CaseTags.Add(new TestCaseTag(Guid.NewGuid(), testCase.Id, tag.Id, accountId, now));
        }
        for (var index = 0; index < request.Steps.Count; index++)
        {
            var step = new TestStep(
                Guid.NewGuid(),
                testCase.Id,
                index + 1,
                request.Steps[index].Action.Trim(),
                request.Steps[index].ExpectedResult.Trim(),
                accountId,
                now);
            dbContext.CaseSteps.Add(step);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (tags is not null)
            await dbContext.Entry(testCase).Collection(x => x.Tags).Query().Include(x => x.Tag).LoadAsync(cancellationToken);
        return TestManagementResult<TestCaseResponse>.Success(ToCaseResponse(testCase));
    }

    public async Task<TestManagementResult<IReadOnlyList<TestPlanResponse>>> ListPlansAsync(
        Guid workspaceId, Guid accountId, CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<IReadOnlyList<TestPlanResponse>>.Failure(
                TestManagementOutcome.NotFound);
        }

        var plans = await PlanQuery().AsNoTracking()
            .Where(x => x.TestWorkspaceId == workspaceId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
        var issues = await ReadableTraceIssuesAsync(
            plans.Select(item => item.TestIssueId), accountId, cancellationToken);
        return TestManagementResult<IReadOnlyList<TestPlanResponse>>.Success(
            plans.Select(item => ToPlanResponse(
                item,
                item.TestIssueId.HasValue
                    ? issues.GetValueOrDefault(item.TestIssueId.Value)
                    : null)).ToArray());
    }

    public async Task<TestManagementResult<TestPlanResponse>> GetPlanAsync(
        Guid workspaceId, Guid planId, Guid accountId, CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<TestPlanResponse>.Failure(TestManagementOutcome.NotFound);
        }

        var plan = await PlanQuery().AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == planId && x.TestWorkspaceId == workspaceId, cancellationToken);
        if (plan is null) return TestManagementResult<TestPlanResponse>.Failure(TestManagementOutcome.NotFound);
        var issue = plan.TestIssueId.HasValue
            ? await issueDirectory.FindReadableAsync(plan.TestIssueId.Value, accountId, cancellationToken)
            : null;
        return TestManagementResult<TestPlanResponse>.Success(ToPlanResponse(plan, issue));
    }

    public async Task<TestManagementResult<TestPlanResponse>> CreatePlanAsync(
        Guid workspaceId, Guid accountId, CreateTestPlanRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestPlanResponse>.Failure(TestManagementOutcome.NotFound);
        }
        if (!CanManageAssets(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestPlanResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var cases = await ResolvePlanCasesAsync(workspaceId, request.CaseIds, cancellationToken);
        if (cases is null)
        {
            return TestManagementResult<TestPlanResponse>.Failure(
                TestManagementOutcome.Invalid,
                "plan_cases_invalid");
        }
        var testIssue = await ResolveTestIssueForWriteAsync(
            workspaceId, accountId, request.TestIssueId, cancellationToken);
        if (testIssue.Code is not null)
        {
            return TestManagementResult<TestPlanResponse>.Failure(
                TestManagementOutcome.Invalid,
                testIssue.Code);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var planNo = await NextNumberAsync(TestNumberType.Plan, workspaceId, cancellationToken);
        var name = PlanName(request.Name, now);
        var plan = new TestPlan(
            Guid.NewGuid(), workspaceId, planNo, name, Clean(request.Description),
            accountId, now, testIssue.Issue?.ProjectId, testIssue.Issue?.Id);
        dbContext.Plans.Add(plan);
        for (var index = 0; index < cases.Count; index++)
        {
            dbContext.PlanItems.Add(new TestPlanItem(
                Guid.NewGuid(), plan.Id, cases[index].Id, index + 1, accountId, now));
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetPlanAsync(workspaceId, plan.Id, accountId, cancellationToken);
    }

    public async Task<TestManagementResult<TestPlanResponse>> UpdatePlanAsync(
        Guid workspaceId, Guid planId, Guid accountId, UpdateTestPlanRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestPlanResponse>.Failure(TestManagementOutcome.NotFound);
        }
        if (!CanManageAssets(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestPlanResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var plan = await dbContext.Plans.Include(x => x.Items).SingleOrDefaultAsync(
            x => x.Id == planId && x.TestWorkspaceId == workspaceId, cancellationToken);
        if (plan is null) return TestManagementResult<TestPlanResponse>.Failure(TestManagementOutcome.NotFound);
        if (plan.Version != request.Version)
        {
            return TestManagementResult<TestPlanResponse>.Failure(
                TestManagementOutcome.Conflict,
                "plan_version_conflict");
        }
        if (plan.Status == TestPlanStatus.Archived)
        {
            return TestManagementResult<TestPlanResponse>.Failure(
                TestManagementOutcome.Conflict,
                "plan_archived");
        }

        if (!TestManagementCodes.TryParsePlanStatus(request.Status, out var status))
        {
            return TestManagementResult<TestPlanResponse>.Failure(
                TestManagementOutcome.Invalid,
                "test_plan_status_invalid");
        }

        var cases = await ResolvePlanCasesAsync(workspaceId, request.CaseIds, cancellationToken);
        if (cases is null || (status == TestPlanStatus.Active && cases.Count == 0))
        {
            return TestManagementResult<TestPlanResponse>.Failure(
                TestManagementOutcome.Invalid,
                "plan_cases_invalid");
        }
        var testIssue = await ResolveTestIssueForWriteAsync(
            workspaceId, accountId, request.TestIssueId, cancellationToken);
        if (testIssue.Code is not null)
        {
            return TestManagementResult<TestPlanResponse>.Failure(
                TestManagementOutcome.Invalid,
                testIssue.Code);
        }

        var now = timeProvider.GetUtcNow();
        foreach (var item in plan.Items.ToArray()) dbContext.PlanItems.Remove(item);
        for (var index = 0; index < cases.Count; index++)
        {
            dbContext.PlanItems.Add(new TestPlanItem(
                Guid.NewGuid(), plan.Id, cases[index].Id, index + 1, accountId, now));
        }
        plan.Update(
            PlanName(request.Name, now), Clean(request.Description), status, accountId, now,
            testIssue.Issue?.ProjectId, testIssue.Issue?.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPlanAsync(workspaceId, plan.Id, accountId, cancellationToken);
    }

    public async Task<TestManagementResult<IReadOnlyList<TestRunResponse>>> ListRunsAsync(
        Guid workspaceId, Guid accountId, CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<IReadOnlyList<TestRunResponse>>.Failure(
                TestManagementOutcome.NotFound);
        }

        var runs = await RunQuery().AsNoTracking()
            .Where(x => dbContext.Plans.Any(
                plan => plan.Id == x.TestPlanId && plan.TestWorkspaceId == workspaceId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var issues = await ReadableTraceIssuesAsync(
            runs.Select(item => item.TestIssueId), accountId, cancellationToken);
        return TestManagementResult<IReadOnlyList<TestRunResponse>>.Success(
            runs.Select(item => ToRunResponse(
                item,
                item.TestIssueId.HasValue
                    ? issues.GetValueOrDefault(item.TestIssueId.Value)
                    : null)).ToArray());
    }

    public async Task<TestManagementResult<TestRunResponse>> GetRunAsync(
        Guid workspaceId, Guid runId, Guid accountId, CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        }

        var run = await RunQuery().AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == runId && dbContext.Plans.Any(
                plan => plan.Id == x.TestPlanId && plan.TestWorkspaceId == workspaceId),
            cancellationToken);
        if (run is null) return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        var issue = run.TestIssueId.HasValue
            ? await issueDirectory.FindReadableAsync(run.TestIssueId.Value, accountId, cancellationToken)
            : null;
        return TestManagementResult<TestRunResponse>.Success(ToRunResponse(run, issue));
    }

    public async Task<TestManagementResult<TestRunResponse>> CreateRunAsync(
        Guid workspaceId, Guid accountId, CreateTestRunRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        }
        if (!CanExecute(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var plan = await dbContext.Plans.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.TestCase).ThenInclude(x => x.Steps)
            .SingleOrDefaultAsync(
                x => x.Id == request.PlanId && x.TestWorkspaceId == workspaceId,
                cancellationToken);
        if (plan is null) return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        if (plan.Status != TestPlanStatus.Active || plan.Items.Count == 0)
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "plan_not_active");
        }
        if (plan.Items.Any(x => x.TestCase.Status != TestAssetStatus.Active))
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "plan_contains_inactive_case");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var runNo = await NextNumberAsync(TestNumberType.Run, plan.Id, cancellationToken);
        var run = new TestRun(
            Guid.NewGuid(), plan.Id, runNo, request.Name.Trim(), accountId, now,
            plan.TestIssueProjectId, plan.TestIssueId);
        dbContext.Runs.Add(run);
        foreach (var planItem in plan.Items.OrderBy(x => x.SortOrder))
        {
            var runItem = new TestRunItem(
                Guid.NewGuid(), run.Id, planItem.TestCase, planItem.SortOrder, accountId, now);
            dbContext.RunItems.Add(runItem);
            dbContext.RunItemStepResults.AddRange(planItem.TestCase.Steps
                .OrderBy(x => x.StepNo)
                .Select(step => new TestRunItemStepResult(
                    Guid.NewGuid(), runItem.Id, step, accountId, now)));
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetRunAsync(workspaceId, run.Id, accountId, cancellationToken);
    }

    public async Task<TestManagementResult<TestRunResponse>> RerunAsync(
        Guid workspaceId, Guid runId, Guid accountId, CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        }
        if (!CanExecute(access.Role) || access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var source = await dbContext.Runs.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == runId && dbContext.Plans.Any(
                plan => plan.Id == x.TestPlanId && plan.TestWorkspaceId == workspaceId),
            cancellationToken);
        if (source is null) return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        if (!IsTerminal(source.Status))
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "run_not_terminal");
        }
        return await CreateRunAsync(workspaceId, accountId,
            new CreateTestRunRequest(source.TestPlanId, $"{source.Name} rerun"), cancellationToken);
    }

    public async Task<TestManagementResult<TestRunResponse>> RecordRunItemAsync(
        Guid workspaceId, Guid runId, Guid itemId, Guid accountId,
        RecordTestResultRequest request, CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        }
        if (!CanExecute(access.Role))
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var run = await dbContext.Runs
            .Include(x => x.Items).ThenInclude(x => x.Steps)
            .SingleOrDefaultAsync(
            x => x.Id == runId && dbContext.Plans.Any(
                plan => plan.Id == x.TestPlanId && plan.TestWorkspaceId == workspaceId),
            cancellationToken);
        if (run is null) return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        if (IsTerminal(run.Status))
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "run_is_terminal");
        }
        var item = run.Items.SingleOrDefault(x => x.Id == itemId);
        if (item is null) return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        if (item.Version != request.Version)
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "run_item_version_conflict");
        }

        var now = timeProvider.GetUtcNow();
        if (!TestManagementCodes.TryParseResultStatus(request.Status, out var resultStatus))
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Invalid,
                "test_result_status_invalid");
        }

        item.Record(resultStatus, Clean(request.ActualResult), accountId, now);
        run.MarkInProgress(accountId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRunAsync(workspaceId, runId, accountId, cancellationToken);
    }

    public async Task<TestManagementResult<TestRunResponse>> RecordRunStepAsync(
        Guid workspaceId, Guid runId, Guid itemId, Guid stepId, Guid accountId,
        RecordTestResultRequest request, CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        }
        if (!CanExecute(access.Role))
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var run = await dbContext.Runs.Include(x => x.Items).ThenInclude(x => x.Steps)
            .SingleOrDefaultAsync(
                x => x.Id == runId && dbContext.Plans.Any(
                    plan => plan.Id == x.TestPlanId && plan.TestWorkspaceId == workspaceId),
                cancellationToken);
        if (run is null) return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        if (IsTerminal(run.Status))
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "run_is_terminal");
        }
        var step = run.Items.SingleOrDefault(x => x.Id == itemId)?.Steps
            .SingleOrDefault(x => x.Id == stepId);
        if (step is null) return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        if (step.Version != request.Version)
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "run_step_version_conflict");
        }

        var now = timeProvider.GetUtcNow();
        if (!TestManagementCodes.TryParseResultStatus(request.Status, out var resultStatus))
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Invalid,
                "test_result_status_invalid");
        }

        step.Record(resultStatus, Clean(request.ActualResult), accountId, now);
        run.MarkInProgress(accountId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRunAsync(workspaceId, runId, accountId, cancellationToken);
    }

    public async Task<TestManagementResult<TestRunResponse>> UpdateRunStatusAsync(
        Guid workspaceId, Guid runId, Guid accountId, UpdateTestRunStatusRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        }
        if (!CanExecute(access.Role))
        {
            return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.Forbidden);
        }

        var run = await dbContext.Runs
            .Include(x => x.Items).ThenInclude(x => x.Steps)
            .SingleOrDefaultAsync(
            x => x.Id == runId && dbContext.Plans.Any(
                plan => plan.Id == x.TestPlanId && plan.TestWorkspaceId == workspaceId),
            cancellationToken);
        if (run is null) return TestManagementResult<TestRunResponse>.Failure(TestManagementOutcome.NotFound);
        if (run.Version != request.Version)
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "run_version_conflict");
        }
        if (!TestManagementCodes.TryParseRunStatus(request.Status, out var status) ||
            status == TestRunStatus.NotStarted)
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Invalid,
                "test_run_status_invalid");
        }

        if (IsTerminal(run.Status) &&
            !(run.Status == TestRunStatus.Cancelled && status == TestRunStatus.InProgress))
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "run_is_terminal");
        }
        if (status == TestRunStatus.InProgress)
        {
            run.MarkInProgress(accountId, timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
            return await GetRunAsync(workspaceId, runId, accountId, cancellationToken);
        }
        if (status == TestRunStatus.Completed && run.Items.Any(x =>
            x.Steps.Count > 0
                ? x.Steps.Any(step => step.ResultStatus == TestResultStatus.NotRun)
                : x.ResultStatus == TestResultStatus.NotRun))
        {
            return TestManagementResult<TestRunResponse>.Failure(
                TestManagementOutcome.Conflict,
                "run_has_unfinished_items");
        }

        run.Finish(status, Clean(request.Summary), accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRunAsync(workspaceId, runId, accountId, cancellationToken);
    }

    private async Task<TestWorkspaceMember?> AccessAsync(
        Guid workspaceId, Guid accountId, CancellationToken cancellationToken) =>
        await dbContext.Members.Include(x => x.Workspace).SingleOrDefaultAsync(
            x => x.TestWorkspaceId == workspaceId && x.AccountId == accountId &&
                x.Status == TestWorkspaceMemberStatus.Active, cancellationToken);

    private async Task<int> ActiveOwnerCountAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        await dbContext.Members.CountAsync(
            x => x.TestWorkspaceId == workspaceId &&
                x.Status == TestWorkspaceMemberStatus.Active &&
                x.Role == TestWorkspaceRole.Owner,
            cancellationToken);

    private async Task<int?> ParentDepthAsync(
        Guid workspaceId, Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null) return 0;
        var suites = await dbContext.Suites.AsNoTracking()
            .Where(x => x.TestWorkspaceId == workspaceId).ToListAsync(cancellationToken);
        return DepthOf(parentId, suites);
    }

    private async Task<bool> HasSuiteNameAsync(
        Guid workspaceId, Guid? parentId, string name, Guid? exceptId,
        CancellationToken cancellationToken) =>
        await dbContext.Suites.AnyAsync(x =>
            x.TestWorkspaceId == workspaceId && x.ParentId == parentId &&
            x.Id != exceptId && x.Name == name.Trim(), cancellationToken);

    private static bool CanManage(TestWorkspaceRole role) =>
        role is TestWorkspaceRole.Owner or TestWorkspaceRole.Manager;

    private static bool CanManageAssets(TestWorkspaceRole role) => CanManage(role);

    private static bool CanExecute(TestWorkspaceRole role) =>
        role is TestWorkspaceRole.Owner or TestWorkspaceRole.Manager or TestWorkspaceRole.Tester;

    private static bool IsTerminal(TestRunStatus status) =>
        status is TestRunStatus.Completed or TestRunStatus.Cancelled;

    private IQueryable<TestPlan> PlanQuery() => dbContext.Plans
        .Include(x => x.Workspace)
        .Include(x => x.Items).ThenInclude(x => x.TestCase);

    private IQueryable<TestRun> RunQuery() => dbContext.Runs
        .Include(x => x.Plan).ThenInclude(x => x.Workspace)
        .Include(x => x.Items).ThenInclude(x => x.Steps);

    private async Task<IReadOnlyList<TestCase>?> ResolvePlanCasesAsync(
        Guid workspaceId, IReadOnlyList<Guid> caseIds, CancellationToken cancellationToken)
    {
        if (caseIds.Count != caseIds.Distinct().Count()) return null;
        var cases = await dbContext.Cases.AsNoTracking()
            .Where(x => caseIds.Contains(x.Id) &&
                x.Suite.TestWorkspaceId == workspaceId && x.Status == TestAssetStatus.Active)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        return cases.Count == caseIds.Count
            ? caseIds.Select(x => cases[x]).ToArray()
            : null;
    }
    private async Task<string> GeneratePrefixAsync(string name, CancellationToken cancellationToken)
    {
        var basePrefix = Regex.Replace(name.ToUpperInvariant(), "[^A-Z0-9]", "");
        if (basePrefix.Length == 0 || !char.IsLetter(basePrefix[0])) basePrefix = "TW";
        basePrefix = basePrefix[..Math.Min(basePrefix.Length, 6)];

        for (var suffix = 0; suffix < 10_000; suffix++)
        {
            var suffixText = suffix == 0 ? "" : (suffix + 1).ToString();
            var stemLength = Math.Min(basePrefix.Length, 10 - suffixText.Length);
            var candidate = basePrefix[..stemLength] + suffixText;
            if (!await dbContext.Workspaces.AnyAsync(x => x.Prefix == candidate, cancellationToken))
                return candidate;
        }

        throw new InvalidOperationException("Unable to generate a unique workspace prefix.");
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private async Task<IReadOnlyList<TestTag>?> ActiveTagsAsync(IReadOnlyList<Guid>? tagIds, CancellationToken cancellationToken)
    {
        var ids = tagIds?.Distinct().ToArray() ?? [];
        var tags = await dbContext.Tags
            .Where(x => ids.Contains(x.Id) && x.Status == TestAssetStatus.Active)
            .ToListAsync(cancellationToken);
        return tags.Count == ids.Length ? tags : null;
    }
    private static string PlanName(string? value, DateTimeOffset now) =>
        string.IsNullOrWhiteSpace(value)
            ? $"TestPlan{now.ToOffset(TimeSpan.FromHours(8)):yyyyMMdd}"
            : value.Trim();
    private static TestWorkspaceResponse ToWorkspaceResponse(
        TestWorkspace workspace,
        TestWorkspaceRole role) =>
        new()
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Prefix = workspace.Prefix,
            Description = workspace.Description,
            Status = workspace.Status.ToCode(),
            CurrentUserRole = role.ToCode(),
            CreatedAt = workspace.CreatedAt,
            UpdatedAt = workspace.UpdatedAt,
            Version = workspace.Version,
        };
    private static TestWorkspaceProjectResponse ToWorkspaceProjectResponse(
        TestWorkspaceProject link,
        ProjectDirectoryEntry project) =>
        new()
        {
            Id = link.Id,
            ProjectId = link.ProjectId,
            Code = project.Code,
            Name = project.Name,
            Status = project.Status.ToCode(),
            LinkedAt = link.CreatedAt,
            Version = link.Version,
        };
    private static TestSuiteResponse ToSuiteResponse(TestSuite x, int depth) =>
        new()
        {
            Id = x.Id,
            ParentId = x.ParentId,
            Name = x.Name,
            Description = x.Description,
            SortOrder = x.SortOrder,
            Status = x.Status.ToCode(),
            Depth = depth,
            Version = x.Version,
        };
    private static TestCaseResponse ToCaseResponse(TestCase x) =>
        new()
        {
            Id = x.Id,
            SuiteId = x.TestSuiteId,
            CaseNo = x.CaseNo,
            Tags = x.Tags.OrderBy(tag => tag.Tag.Name).Select(tag => new TestTagResponse
            {
                Id = tag.Tag.Id,
                Name = tag.Tag.Name,
                Description = tag.Tag.Description,
                Status = tag.Tag.Status.ToCode(),
                Version = tag.Tag.Version,
            }).ToArray(),
            Title = x.Title,
            Description = x.Description,
            Preconditions = x.Preconditions,
            OverallExpectedResult = x.OverallExpectedResult,
            SortOrder = x.SortOrder,
            Status = x.Status.ToCode(),
            Steps = x.Steps.OrderBy(step => step.StepNo)
                .Select(step => new TestCaseStepResponse
                {
                    Id = step.Id,
                    StepNo = step.StepNo,
                    Action = step.Action,
                    ExpectedResult = step.ExpectedResult,
                })
                .ToArray(),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            Version = x.Version,
        };
    private static TestTagResponse ToTagResponse(TestTag x) =>
        new()
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Status = x.Status.ToCode(),
            Version = x.Version,
        };
    private async Task<int> NextNumberAsync(
        TestNumberType numberType,
        Guid scopeId,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsNpgsql())
        {
            return await dbContext.Database
                .SqlQuery<int>(
                    $"SELECT public.next_test_number({numberType.ToCode()}, {scopeId}) AS \"Value\"")
                .SingleAsync(cancellationToken);
        }

        return numberType switch
        {
            TestNumberType.Case => (await dbContext.Cases
                .Where(x => x.TestWorkspaceId == scopeId)
                .MaxAsync(x => (int?)x.CaseNo, cancellationToken) ?? 0) + 1,
            TestNumberType.Plan => (await dbContext.Plans
                .Where(x => x.TestWorkspaceId == scopeId)
                .MaxAsync(x => (int?)x.PlanNo, cancellationToken) ?? 0) + 1,
            TestNumberType.Run => (await dbContext.Runs
                .Where(x => x.TestPlanId == scopeId)
                .MaxAsync(x => (int?)x.RunNo, cancellationToken) ?? 0) + 1,
            _ => throw new ArgumentOutOfRangeException(
                nameof(numberType),
                numberType,
                "Unsupported test number counter type."),
        };
    }
    private static TestPlanResponse ToPlanResponse(TestPlan x, IssueDirectoryEntry? testIssue) =>
        new()
        {
            Id = x.Id,
            WorkspaceId = x.TestWorkspaceId,
            PlanNo = x.PlanNo,
            Code = $"{x.Workspace.Prefix}-TP{x.PlanNo}",
            Name = x.Name,
            Description = x.Description,
            Status = x.Status.ToCode(),
            Items = x.Items.OrderBy(item => item.SortOrder).Select(item =>
                new TestPlanItemResponse
                {
                    Id = item.Id,
                    CaseId = item.TestCaseId,
                    SortOrder = item.SortOrder,
                    CaseTitle = item.TestCase.Title,
                }).ToArray(),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            Version = x.Version,
            TestIssue = testIssue is null ? null : ToTraceIssueResponse(testIssue),
        };
    private static TestRunResponse ToRunResponse(TestRun x, IssueDirectoryEntry? testIssue)
    {
        var orderedItems = x.Items.OrderBy(item => item.SortOrder).ToArray();
        var progress = new TestRunProgressResponse
        {
            Total = orderedItems.Length,
            NotRun = orderedItems.Count(item => item.ResultStatus == TestResultStatus.NotRun),
            Passed = orderedItems.Count(item => item.ResultStatus == TestResultStatus.Passed),
            Failed = orderedItems.Count(item => item.ResultStatus == TestResultStatus.Failed),
            Blocked = orderedItems.Count(item => item.ResultStatus == TestResultStatus.Blocked),
            Skipped = orderedItems.Count(item => item.ResultStatus == TestResultStatus.Skipped),
        };
        var items = orderedItems.Select(item =>
            new TestRunItemResponse
            {
                Id = item.Id,
                CaseId = item.TestCaseId,
                SortOrder = item.SortOrder,
                CaseTitle = item.CaseTitle,
                CaseDescription = item.CaseDescription,
                Preconditions = item.Preconditions,
                OverallExpectedResult = item.OverallExpectedResult,
                ResultStatus = item.ResultStatus.ToCode(),
                ActualResult = item.ActualResult,
                ExecutedByAccountId = item.ExecutedByAccountId,
                ExecutedAt = item.ExecutedAt,
                Steps = item.Steps.OrderBy(step => step.StepNo).Select(step =>
                    new TestRunStepResponse
                    {
                        Id = step.Id,
                        StepNo = step.StepNo,
                        Action = step.Action,
                        ExpectedResult = step.ExpectedResult,
                        ResultStatus = step.ResultStatus.ToCode(),
                        ActualResult = step.ActualResult,
                        ExecutedByAccountId = step.ExecutedByAccountId,
                        ExecutedAt = step.ExecutedAt,
                        Version = step.Version,
                    }).ToArray(),
                Version = item.Version,
            }).ToArray();
        return new TestRunResponse
        {
            Id = x.Id,
            PlanId = x.TestPlanId,
            RunNo = x.RunNo,
            Code = $"{x.Plan.Workspace.Prefix}-TP{x.Plan.PlanNo}-R{x.RunNo}",
            Name = x.Name,
            Status = x.Status.ToCode(),
            StartedByAccountId = x.StartedByAccountId,
            StartedAt = x.StartedAt,
            CompletedAt = x.CompletedAt,
            Summary = x.Summary,
            Progress = progress,
            Items = items,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            Version = x.Version,
            TestIssue = testIssue is null ? null : ToTraceIssueResponse(testIssue),
        };
    }

    private async Task<(IssueDirectoryEntry? Issue, string? Code)> ResolveTestIssueForWriteAsync(
        Guid workspaceId,
        Guid accountId,
        Guid? issueId,
        CancellationToken cancellationToken)
    {
        if (!issueId.HasValue) return (null, null);

        var issue = await issueDirectory.FindUpdatableAsync(
            issueId.Value, accountId, cancellationToken);
        if (issue is null) return (null, "test_issue_not_accessible");
        if (issue.ProjectStatus != ProjectStatus.Active) return (null, "project_not_active");
        if (issue.TypeCode != "task") return (null, "test_issue_type_invalid");

        var linked = await dbContext.WorkspaceProjects.AnyAsync(
            item => item.TestWorkspaceId == workspaceId && item.ProjectId == issue.ProjectId,
            cancellationToken);
        return linked ? (issue, null) : (null, "workspace_project_not_linked");
    }

    private async Task<IReadOnlyDictionary<Guid, IssueDirectoryEntry>> ReadableTraceIssuesAsync(
        IEnumerable<Guid?> issueIds,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var ids = issueIds.Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        return await issueDirectory.GetReadableByIdsAsync(ids, accountId, cancellationToken);
    }

    private static TestTraceIssueResponse ToTraceIssueResponse(IssueDirectoryEntry issue) =>
        new()
        {
            Id = issue.Id,
            ProjectId = issue.ProjectId,
            ProjectCode = issue.ProjectCode,
            IssueNo = issue.IssueNo,
            Key = issue.Key,
            Title = issue.Title,
            TypeCode = issue.TypeCode,
            StatusCode = issue.StatusCode,
        };
    private static IReadOnlyList<TestSuiteResponse> MapSuites(IReadOnlyList<TestSuite> suites) =>
        suites.Select(x => ToSuiteResponse(x, DepthOf(x.Id, suites) ?? 1)).ToArray();

    private static int? DepthOf(Guid? id, IReadOnlyList<TestSuite> suites)
    {
        if (id is null) return 0;
        var byId = suites.ToDictionary(x => x.Id);
        var seen = new HashSet<Guid>();
        var current = id;
        var depth = 0;
        while (current is not null)
        {
            if (!seen.Add(current.Value) || !byId.TryGetValue(current.Value, out var suite)) return null;
            depth++;
            current = suite.ParentId;
        }
        return depth;
    }

    private static int HeightOf(Guid id, IReadOnlyList<TestSuite> suites)
    {
        var children = suites.Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToArray());
        int Height(Guid current) => children.GetValueOrDefault(current) is { Length: > 0 } values
            ? 1 + values.Max(Height) : 1;
        return Height(id);
    }

    private static HashSet<Guid> DescendantIds(Guid id, IReadOnlyList<TestSuite> suites)
    {
        var result = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(id);
        while (queue.TryDequeue(out var current))
        {
            foreach (var child in suites.Where(x => x.ParentId == current))
            {
                if (result.Add(child.Id)) queue.Enqueue(child.Id);
            }
        }
        return result;
    }
}
