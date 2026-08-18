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
            return TestManagementResult<IReadOnlyList<TestRunBugLinkResponse>>.Failure(
                TestManagementOutcome.NotFound);
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
        return TestManagementResult<IReadOnlyList<TestRunBugLinkResponse>>.Success(response);
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
                item.Status == TestWorkspaceMemberStatus.Active,
            cancellationToken);
        if (access is null || !await RunExistsAsync(workspaceId, runId, cancellationToken))
        {
            return TestManagementResult<TestRunBugLinkResponse>.Failure(
                TestManagementOutcome.NotFound);
        }

        if (access.Role is not (TestWorkspaceRole.Owner or TestWorkspaceRole.Manager) ||
            access.Workspace.Status != TestAssetStatus.Active)
        {
            return TestManagementResult<TestRunBugLinkResponse>.Failure(
                TestManagementOutcome.Forbidden);
        }

        if (!await dbContext.WorkspaceProjects.AnyAsync(
            item => item.TestWorkspaceId == workspaceId && item.ProjectId == request.ProjectId,
            cancellationToken))
        {
            return TestManagementResult<TestRunBugLinkResponse>.Failure(
                TestManagementOutcome.Invalid,
                "workspace_project_not_linked");
        }

        var issueResult = await issueCommandService.CreateAsync(
            request.ProjectId,
            accountId,
            new CreateIssueCommand(
                title: request.Title,
                typeCode: "bug")
            {
                PriorityCode = request.PriorityCode,
                Description = request.Description,
                AssigneeAccountId = request.AssigneeAccountId,
            },
            cancellationToken);
        if (issueResult.Outcome != CreateIssueCommandOutcome.Succeeded || issueResult.Issue is null)
        {
            return issueResult.Outcome switch
            {
                CreateIssueCommandOutcome.Forbidden =>
                    TestManagementResult<TestRunBugLinkResponse>.Failure(
                        TestManagementOutcome.Forbidden),
                CreateIssueCommandOutcome.NotFound =>
                    TestManagementResult<TestRunBugLinkResponse>.Failure(
                        TestManagementOutcome.NotFound),
                CreateIssueCommandOutcome.InvalidOption =>
                    TestManagementResult<TestRunBugLinkResponse>.Failure(
                        TestManagementOutcome.Invalid,
                        "bug_issue_option_invalid"),
                CreateIssueCommandOutcome.InvalidAssignee =>
                    TestManagementResult<TestRunBugLinkResponse>.Failure(
                        TestManagementOutcome.Invalid,
                        "bug_issue_assignee_invalid"),
                CreateIssueCommandOutcome.ProjectInactive =>
                    TestManagementResult<TestRunBugLinkResponse>.Failure(
                        TestManagementOutcome.Conflict,
                        "project_not_active"),
                _ => TestManagementResult<TestRunBugLinkResponse>.Failure(
                    TestManagementOutcome.Conflict,
                    "bug_issue_create_conflict"),
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
            return TestManagementResult<TestRunBugLinkResponse>.Failure(
                TestManagementOutcome.Conflict,
                "run_bug_link_conflict");
        }

        return TestManagementResult<TestRunBugLinkResponse>.Success(
            ToResponse(link, issueResult.Issue));
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
        new()
        {
            Id = link.Id,
            TestRunId = link.TestRunId,
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
