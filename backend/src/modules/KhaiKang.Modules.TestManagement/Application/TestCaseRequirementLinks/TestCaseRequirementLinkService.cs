using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.TestManagement.Contracts;
using KhaiKang.Modules.TestManagement.Domain;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.TestManagement.Application;

public sealed class TestCaseRequirementLinkService(
    TestManagementDbContext dbContext,
    IIssueDirectory issueDirectory,
    TimeProvider timeProvider)
{
    public async Task<TestManagementResult<IReadOnlyList<TestCaseRequirementLinkResponse>>> ListAsync(
        Guid workspaceId,
        Guid caseId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasWorkspaceAccessAsync(workspaceId, accountId, cancellationToken) ||
            !await CaseExistsAsync(workspaceId, caseId, cancellationToken))
        {
            return TestManagementResult<IReadOnlyList<TestCaseRequirementLinkResponse>>.Failure(
                TestManagementOutcome.NotFound);
        }

        var links = await dbContext.CaseRequirementLinks.AsNoTracking()
            .Where(item => item.TestWorkspaceId == workspaceId &&
                item.TestCaseId == caseId &&
                !item.IsDeleted)
            .ToArrayAsync(cancellationToken);
        var issues = await issueDirectory.GetReadableByIdsAsync(
            links.Select(item => item.RequirementIssueId).ToArray(),
            accountId,
            cancellationToken);
        var response = links
            .Where(item => issues.ContainsKey(item.RequirementIssueId))
            .OrderBy(item => issues[item.RequirementIssueId].Key)
            .Select(item => ToResponse(item, issues[item.RequirementIssueId]))
            .ToArray();
        return TestManagementResult<IReadOnlyList<TestCaseRequirementLinkResponse>>.Success(response);
    }

    public async Task<TestManagementResult<TestCaseRequirementLinkResponse>> CreateAsync(
        Guid workspaceId,
        Guid caseId,
        Guid accountId,
        LinkTestCaseRequirementIssueRequest request,
        CancellationToken cancellationToken)
    {
        var access = await dbContext.Members.Include(item => item.Workspace).SingleOrDefaultAsync(
            item => item.TestWorkspaceId == workspaceId &&
                item.AccountId == accountId &&
                item.Status == TestWorkspaceMemberStatus.Active,
            cancellationToken);
        if (access is null || !await CaseExistsAsync(workspaceId, caseId, cancellationToken))
        {
            return TestManagementResult<TestCaseRequirementLinkResponse>.Failure(
                TestManagementOutcome.NotFound);
        }

        if (access.Role is not (TestWorkspaceRole.Owner or TestWorkspaceRole.Manager) ||
            access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestCaseRequirementLinkResponse>.Failure(
                TestManagementOutcome.Forbidden);
        }

        var issue = await issueDirectory.FindUpdatableAsync(
            request.RequirementIssueId, accountId, cancellationToken);
        if (issue is null)
        {
            return TestManagementResult<TestCaseRequirementLinkResponse>.Failure(
                TestManagementOutcome.NotFound);
        }

        if (issue.ProjectStatus != ProjectStatus.Active)
        {
            return TestManagementResult<TestCaseRequirementLinkResponse>.Failure(
                TestManagementOutcome.Conflict,
                "project_not_active");
        }

        if (!await dbContext.WorkspaceProjects.AnyAsync(
            item => item.TestWorkspaceId == workspaceId && item.ProjectId == issue.ProjectId,
            cancellationToken))
        {
            return TestManagementResult<TestCaseRequirementLinkResponse>.Failure(
                TestManagementOutcome.Invalid,
                "workspace_project_not_linked");
        }

        if (await dbContext.CaseRequirementLinks.AnyAsync(
            item => item.TestCaseId == caseId &&
                item.RequirementIssueId == issue.Id &&
                !item.IsDeleted,
            cancellationToken))
        {
            return TestManagementResult<TestCaseRequirementLinkResponse>.Failure(
                TestManagementOutcome.Conflict,
                "case_requirement_link_duplicate");
        }

        var now = timeProvider.GetUtcNow();
        var link = new TestCaseRequirementLink(
            Guid.NewGuid(), workspaceId, caseId, issue.ProjectId, issue.Id, accountId, now);
        dbContext.CaseRequirementLinks.Add(link);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TestManagementResult<TestCaseRequirementLinkResponse>.Failure(
                TestManagementOutcome.Conflict,
                "case_requirement_link_duplicate");
        }

        return TestManagementResult<TestCaseRequirementLinkResponse>.Success(
            ToResponse(link, issue));
    }

    public async Task<TestManagementResult<object>> DeleteAsync(
        Guid workspaceId,
        Guid caseId,
        Guid linkId,
        Guid accountId,
        int version,
        CancellationToken cancellationToken)
    {
        var access = await dbContext.Members.Include(item => item.Workspace).SingleOrDefaultAsync(
            item => item.TestWorkspaceId == workspaceId &&
                item.AccountId == accountId &&
                item.Status == TestWorkspaceMemberStatus.Active,
            cancellationToken);
        if (access is null)
        {
            return TestManagementResult<object>.Failure(TestManagementOutcome.NotFound);
        }

        if (access.Role is not (TestWorkspaceRole.Owner or TestWorkspaceRole.Manager) ||
            access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<object>.Failure(TestManagementOutcome.Forbidden);
        }

        var link = await dbContext.CaseRequirementLinks.SingleOrDefaultAsync(
            item => item.Id == linkId &&
                item.TestWorkspaceId == workspaceId &&
                item.TestCaseId == caseId &&
                !item.IsDeleted,
            cancellationToken);
        if (link is null || await issueDirectory.FindUpdatableAsync(
            link.RequirementIssueId, accountId, cancellationToken) is null)
        {
            return TestManagementResult<object>.Failure(TestManagementOutcome.NotFound);
        }

        if (link.Version != version)
        {
            return TestManagementResult<object>.Failure(
                TestManagementOutcome.Conflict,
                "case_requirement_link_version_conflict");
        }

        link.Delete(accountId, timeProvider.GetUtcNow());
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TestManagementResult<object>.Failure(
                TestManagementOutcome.Conflict,
                "case_requirement_link_version_conflict");
        }

        return TestManagementResult<object>.Success(new object());
    }

    private Task<bool> HasWorkspaceAccessAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken) =>
        dbContext.Members.AnyAsync(
            item => item.TestWorkspaceId == workspaceId &&
                item.AccountId == accountId &&
                item.Status == TestWorkspaceMemberStatus.Active,
            cancellationToken);

    private Task<bool> CaseExistsAsync(
        Guid workspaceId,
        Guid caseId,
        CancellationToken cancellationToken) =>
        dbContext.Cases.AnyAsync(
            item => item.Id == caseId && item.TestWorkspaceId == workspaceId,
            cancellationToken);

    private static TestCaseRequirementLinkResponse ToResponse(
        TestCaseRequirementLink link,
        IssueDirectoryEntry issue) =>
        new()
        {
            Id = link.Id,
            TestCaseId = link.TestCaseId,
            Issue = new TestTraceIssueResponse
            {
                Id = issue.Id,
                ProjectId = issue.ProjectId,
                ProjectCode = issue.ProjectCode,
                IssueNo = issue.IssueNo,
                Key = issue.Key,
                Title = issue.Title,
                TypeCode = issue.TypeCode,
                StatusCode = issue.StatusCode,
            },
            CreatedAt = link.CreatedAt,
            Version = link.Version,
        };
}
