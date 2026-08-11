using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;
using KhaiKang.Modules.TestManagement.Domain;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.TestManagement.Application;

public sealed class TestRunBugService(
    TestManagementDbContext dbContext,
    IIssueDirectory issueDirectory,
    IIssueCommandService issueCommandService,
    TimeProvider timeProvider)
{
    public async Task<TestManagementResult<IReadOnlyList<TestRunBugLinkResponse>>> ListAsync(
        Guid workspaceId,
        Guid runId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasWorkspaceAccessAsync(workspaceId, accountId, cancellationToken) ||
            !await RunExistsAsync(workspaceId, runId, cancellationToken))
        {
            return new(TestManagementOutcome.NotFound);
        }

        var links = await dbContext.RunBugLinks.AsNoTracking()
            .Where(item => item.TestWorkspaceId == workspaceId && item.TestRunId == runId)
            .ToArrayAsync(cancellationToken);
        var issues = await issueDirectory.GetReadableByIdsAsync(
            links.Select(item => item.BugIssueId).ToArray(),
            accountId,
            cancellationToken);
        var response = links
            .Where(item => issues.ContainsKey(item.BugIssueId))
            .OrderBy(item => issues[item.BugIssueId].Key)
            .Select(item => ToResponse(item, issues[item.BugIssueId]))
            .ToArray();
        return new(TestManagementOutcome.Succeeded, response);
    }

    public async Task<TestManagementResult<TestRunBugLinkResponse>> CreateAsync(
        Guid workspaceId,
        Guid runId,
        Guid accountId,
        CreateTestRunBugRequest request,
        CancellationToken cancellationToken)
    {
        var access = await dbContext.Members.Include(item => item.Workspace).SingleOrDefaultAsync(
            item => item.TestWorkspaceId == workspaceId &&
                item.AccountId == accountId &&
                item.Status == "active",
            cancellationToken);
        if (access is null || !await RunExistsAsync(workspaceId, runId, cancellationToken))
        {
            return new(TestManagementOutcome.NotFound);
        }

        if (access.Role is not ("owner" or "manager") || access.Workspace.Status != "active")
        {
            return new(TestManagementOutcome.Forbidden);
        }

        if (!await dbContext.WorkspaceProjects.AnyAsync(
            item => item.TestWorkspaceId == workspaceId && item.ProjectId == request.ProjectId,
            cancellationToken))
        {
            return new(TestManagementOutcome.Invalid, Code: "workspace_project_not_linked");
        }

        var issueResult = await issueCommandService.CreateAsync(
            request.ProjectId,
            accountId,
            new IssueCommandRequest(
                request.Title,
                "bug",
                request.PriorityCode,
                request.Description,
                request.AssigneeAccountId),
            cancellationToken);
        if (issueResult.Outcome != IssueCommandOutcome.Succeeded || issueResult.Issue is null)
        {
            return issueResult.Outcome switch
            {
                IssueCommandOutcome.Forbidden => new(TestManagementOutcome.Forbidden),
                IssueCommandOutcome.NotFound => new(TestManagementOutcome.NotFound),
                IssueCommandOutcome.InvalidOption =>
                    new(TestManagementOutcome.Invalid, Code: "bug_issue_option_invalid"),
                IssueCommandOutcome.InvalidAssignee =>
                    new(TestManagementOutcome.Invalid, Code: "bug_issue_assignee_invalid"),
                IssueCommandOutcome.ProjectInactive =>
                    new(TestManagementOutcome.Conflict, Code: "project_not_active"),
                _ => new(TestManagementOutcome.Conflict, Code: "bug_issue_create_conflict"),
            };
        }

        var now = timeProvider.GetUtcNow();
        var link = new TestRunBugLink(
            Guid.NewGuid(),
            workspaceId,
            runId,
            issueResult.Issue.ProjectId,
            issueResult.Issue.Id,
            accountId,
            now);
        dbContext.RunBugLinks.Add(link);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new(TestManagementOutcome.Conflict, Code: "run_bug_link_conflict");
        }

        return new(TestManagementOutcome.Succeeded, ToResponse(link, issueResult.Issue));
    }

    private Task<bool> HasWorkspaceAccessAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken) =>
        dbContext.Members.AnyAsync(
            item => item.TestWorkspaceId == workspaceId &&
                item.AccountId == accountId &&
                item.Status == "active",
            cancellationToken);

    private Task<bool> RunExistsAsync(
        Guid workspaceId,
        Guid runId,
        CancellationToken cancellationToken) =>
        dbContext.Runs.AnyAsync(
            run => run.Id == runId && dbContext.Plans.Any(
                plan => plan.Id == run.TestPlanId && plan.TestWorkspaceId == workspaceId),
            cancellationToken);

    private static TestRunBugLinkResponse ToResponse(
        TestRunBugLink link,
        IssueDirectoryEntry issue) =>
        new(
            link.Id,
            link.TestRunId,
            new TestTraceIssueResponse(
                issue.Id,
                issue.ProjectId,
                issue.ProjectCode,
                issue.IssueNo,
                issue.Key,
                issue.Title,
                issue.TypeCode,
                issue.StatusCode),
            link.CreatedAt,
            link.Version);
}
