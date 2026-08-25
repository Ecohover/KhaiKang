using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.TestManagement.Contracts;
using KhaiKang.Modules.TestManagement.Domain;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KhaiKang.Modules.TestManagement.Application;

public sealed class TraceabilitySourceService(
    TestManagementDbContext dbContext,
    IIssueDirectory issueDirectory,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TraceabilitySourceResult<TestCaseSourceResponse>> GetCaseSourceAsync(
        Guid workspaceId, Guid caseId, Guid accountId, CancellationToken cancellationToken)
    {
        if (!await HasReadAccessAsync(workspaceId, accountId, cancellationToken))
            return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.NotFound);
        var link = await dbContext.CaseSourceLinks.AsNoTracking().SingleOrDefaultAsync(
            x => x.TestWorkspaceId == workspaceId && x.TestCaseId == caseId, cancellationToken);
        if (link is null) return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.NotFound);
        var issue = await issueDirectory.FindReadableAsync(link.TestIssueId, accountId, cancellationToken);
        return issue is null
            ? TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.NotFound)
            : TraceabilitySourceResult<TestCaseSourceResponse>.Success(ToCaseResponse(link, issue));
    }

    public Task<TraceabilitySourceResult<TestCaseSourceResponse>> CreateCaseSourceAsync(
        Guid workspaceId, Guid caseId, Guid accountId, string key,
        LinkTestCaseSourceRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(
            accountId,
            key,
            Fingerprint(new { operation = "case-source", workspaceId, caseId, request.TestIssueId }),
            () => CreateCaseSourceCoreAsync(workspaceId, caseId, accountId, request, cancellationToken),
            cancellationToken);

    public async Task<TraceabilitySourceResult<BugSourcesResponse>> GetBugSourcesAsync(
        Guid workspaceId, Guid bugIssueId, Guid accountId, CancellationToken cancellationToken)
    {
        if (!await HasReadAccessAsync(workspaceId, accountId, cancellationToken))
            return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.NotFound);
        var bug = await issueDirectory.FindReadableAsync(bugIssueId, accountId, cancellationToken);
        if (bug is null || bug.TypeCode != "bug" ||
            !await IsLinkedProjectAsync(workspaceId, bug.ProjectId, cancellationToken))
            return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.NotFound);
        return TraceabilitySourceResult<BugSourcesResponse>.Success(
            await BuildBugResponseAsync(workspaceId, bug, accountId, cancellationToken));
    }

    public Task<TraceabilitySourceResult<BugSourcesResponse>> CreateBugSourcesAsync(
        Guid workspaceId, Guid bugIssueId, Guid accountId, string key,
        LinkBugSourcesRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(
            accountId,
            key,
            Fingerprint(new
            {
                operation = "bug-sources",
                workspaceId,
                bugIssueId,
                request.TestIssueId,
                request.TestRunId,
                request.TestRunItemId,
            }),
            () => CreateBugSourcesCoreAsync(workspaceId, bugIssueId, accountId, request, cancellationToken),
            cancellationToken);

    private async Task<TraceabilitySourceResult<TestCaseSourceResponse>> CreateCaseSourceCoreAsync(
        Guid workspaceId, Guid caseId, Guid accountId, LinkTestCaseSourceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TestIssueId == Guid.Empty)
            return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.Invalid, "test_issue_required");
        var access = await GetMutationAccessAsync(workspaceId, accountId, cancellationToken);
        if (access == TestManagementOutcome.NotFound ||
            !await dbContext.Cases.AnyAsync(x => x.Id == caseId && x.TestWorkspaceId == workspaceId, cancellationToken))
            return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.NotFound);
        if (access == TestManagementOutcome.Forbidden)
            return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.Forbidden);
        var issue = await issueDirectory.FindUpdatableAsync(request.TestIssueId, accountId, cancellationToken);
        if (issue is null) return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.NotFound);
        if (issue.TypeCode != "task")
            return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.Invalid, "source_issue_not_test_issue");
        if (issue.ProjectStatus != ProjectStatus.Active)
            return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.Conflict, "project_not_active");
        if (!await IsLinkedProjectAsync(workspaceId, issue.ProjectId, cancellationToken))
            return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.Invalid, "workspace_project_not_linked");
        if (await dbContext.CaseSourceLinks.AnyAsync(x => x.TestCaseId == caseId, cancellationToken))
            return TraceabilitySourceResult<TestCaseSourceResponse>.Failure(TestManagementOutcome.Conflict, "case_source_immutable");
        var link = new TestCaseSourceLink(Guid.NewGuid(), workspaceId, caseId, issue.ProjectId, issue.Id, accountId, timeProvider.GetUtcNow());
        dbContext.CaseSourceLinks.Add(link);
        return TraceabilitySourceResult<TestCaseSourceResponse>.Success(ToCaseResponse(link, issue));
    }

    private async Task<TraceabilitySourceResult<BugSourcesResponse>> CreateBugSourcesCoreAsync(
        Guid workspaceId, Guid bugIssueId, Guid accountId, LinkBugSourcesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TestIssueId is null && request.TestRunId is null && request.TestRunItemId is null)
            return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Invalid, "source_required");
        var access = await GetMutationAccessAsync(workspaceId, accountId, cancellationToken);
        if (access == TestManagementOutcome.NotFound)
            return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.NotFound);
        if (access == TestManagementOutcome.Forbidden)
            return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Forbidden);
        var bug = await issueDirectory.FindUpdatableAsync(bugIssueId, accountId, cancellationToken);
        if (bug is null || bug.TypeCode != "bug")
            return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.NotFound);
        if (bug.ProjectStatus != ProjectStatus.Active)
            return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Conflict, "project_not_active");
        if (!await IsLinkedProjectAsync(workspaceId, bug.ProjectId, cancellationToken))
            return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Invalid, "workspace_project_not_linked");

        IssueDirectoryEntry? testIssue = null;
        if (request.TestIssueId is { } testIssueId)
        {
            testIssue = await issueDirectory.FindUpdatableAsync(testIssueId, accountId, cancellationToken);
            if (testIssue is null) return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.NotFound);
            if (testIssue.TypeCode != "task")
                return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Invalid, "source_issue_not_test_issue");
            if (testIssue.ProjectId != bug.ProjectId)
                return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Invalid, "source_project_mismatch");
            if (await dbContext.BugTestIssueSourceLinks.AnyAsync(x => x.BugIssueId == bugIssueId, cancellationToken))
                return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Conflict, "bug_test_issue_source_immutable");
        }

        TestRun? run = null;
        var createRunLink = false;
        if (request.TestRunId is { } runId)
        {
            run = await dbContext.Runs.Include(x => x.Plan).SingleOrDefaultAsync(
                x => x.Id == runId && x.Plan.TestWorkspaceId == workspaceId, cancellationToken);
            if (run is null) return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.NotFound);
            var existingRunSource = await dbContext.RunBugLinks.AsNoTracking()
                .SingleOrDefaultAsync(x => x.BugIssueId == bugIssueId, cancellationToken);
            if (existingRunSource is not null &&
                (existingRunSource.TestWorkspaceId != workspaceId || existingRunSource.TestRunId != runId))
                return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Conflict, "bug_test_run_source_immutable");
            createRunLink = existingRunSource is null;
            if (testIssue is not null && run.TestIssueId is not null && run.TestIssueId != testIssue.Id)
                return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Invalid, "run_test_issue_mismatch");
        }

        if (request.TestRunItemId is { } itemId)
        {
            if (run is null)
                return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Invalid, "test_run_required_for_item");
            if (!await dbContext.RunItems.AnyAsync(x => x.Id == itemId && x.TestRunId == run.Id, cancellationToken))
                return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Invalid, "run_item_not_in_run");
            if (await dbContext.BugTestRunItemSourceLinks.AnyAsync(x => x.BugIssueId == bugIssueId, cancellationToken))
                return TraceabilitySourceResult<BugSourcesResponse>.Failure(TestManagementOutcome.Conflict, "bug_test_run_item_source_immutable");
        }

        var now = timeProvider.GetUtcNow();
        if (testIssue is not null)
            dbContext.BugTestIssueSourceLinks.Add(new(Guid.NewGuid(), workspaceId, bug.ProjectId, bug.Id, testIssue.Id, accountId, now));
        if (run is not null && createRunLink)
            dbContext.RunBugLinks.Add(new TestRunBugLink(Guid.NewGuid(), workspaceId, run.Id, bug.ProjectId, bug.Id, accountId, now));
        if (request.TestRunItemId is { } sourceItemId)
            dbContext.BugTestRunItemSourceLinks.Add(new(Guid.NewGuid(), workspaceId, bug.ProjectId, bug.Id, run!.Id, sourceItemId, accountId, now));

        return TraceabilitySourceResult<BugSourcesResponse>.Success(new BugSourcesResponse
        {
            Bug = ToIssue(bug),
            TestIssue = testIssue is null ? null : ToIssue(testIssue),
            TestRunId = run?.Id,
            TestRunItemId = request.TestRunItemId,
        });
    }

    private async Task<TraceabilitySourceResult<T>> ExecuteAsync<T>(
        Guid accountId, string key, string fingerprint,
        Func<Task<TraceabilitySourceResult<T>>> action, CancellationToken cancellationToken) where T : class
    {
        await ReserveAsync(accountId, key, fingerprint, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await LockAsync(accountId, key, cancellationToken);
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(record.RequestFingerprint), Convert.FromHexString(fingerprint)))
            return TraceabilitySourceResult<T>.Failure(TestManagementOutcome.Conflict, "idempotency-conflict");
        if (record.State == "terminal_success")
            return TraceabilitySourceResult<T>.Success(JsonSerializer.Deserialize<T>(record.ResponseSnapshot!, JsonOptions)!);
        if (record.State == "terminal_business_rejection")
        {
            var rejection = JsonSerializer.Deserialize<Rejection>(record.RejectionSnapshot!, JsonOptions)!;
            return TraceabilitySourceResult<T>.Failure(rejection.Outcome, rejection.Code);
        }
        var result = await action();
        if (result.Outcome == TestManagementOutcome.Succeeded)
            record.CompleteSuccess(JsonSerializer.Serialize(result.Value, JsonOptions), timeProvider.GetUtcNow());
        else
            record.CompleteRejection(JsonSerializer.Serialize(new Rejection(result.Outcome, result.Code), JsonOptions), timeProvider.GetUtcNow());
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TraceabilitySourceResult<T>.Failure(TestManagementOutcome.Conflict, "traceability_source_conflict");
        }
        return result;
    }

    private async Task ReserveAsync(Guid actorId, string key, string fingerprint, CancellationToken cancellationToken)
    {
        if (await dbContext.TraceabilityIdempotencyRecords.AsNoTracking().AnyAsync(x => x.ActorAccountId == actorId && x.IdempotencyKey == key, cancellationToken)) return;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.TraceabilityIdempotencyRecords.Add(new(Guid.NewGuid(), actorId, key, fingerprint, timeProvider.GetUtcNow()));
        try { await dbContext.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { await transaction.RollbackAsync(cancellationToken); }
        finally { dbContext.ChangeTracker.Clear(); }
    }

    private Task<TraceabilityIdempotencyRecord> LockAsync(Guid actorId, string key, CancellationToken cancellationToken) =>
        dbContext.Database.IsNpgsql()
            ? dbContext.TraceabilityIdempotencyRecords.FromSqlInterpolated($"SELECT * FROM traceability_idempotency_records WHERE actor_account_id = {actorId} AND idempotency_key = {key} FOR UPDATE").SingleAsync(cancellationToken)
            : dbContext.TraceabilityIdempotencyRecords.SingleAsync(x => x.ActorAccountId == actorId && x.IdempotencyKey == key, cancellationToken);

    private async Task<BugSourcesResponse> BuildBugResponseAsync(Guid workspaceId, IssueDirectoryEntry bug, Guid accountId, CancellationToken cancellationToken)
    {
        var issueLink = await dbContext.BugTestIssueSourceLinks.AsNoTracking().SingleOrDefaultAsync(x => x.TestWorkspaceId == workspaceId && x.BugIssueId == bug.Id, cancellationToken);
        var runLink = await dbContext.RunBugLinks.AsNoTracking().SingleOrDefaultAsync(x => x.TestWorkspaceId == workspaceId && x.BugIssueId == bug.Id, cancellationToken);
        var itemLink = await dbContext.BugTestRunItemSourceLinks.AsNoTracking().SingleOrDefaultAsync(x => x.TestWorkspaceId == workspaceId && x.BugIssueId == bug.Id, cancellationToken);
        var testIssue = issueLink is null ? null : await issueDirectory.FindReadableAsync(issueLink.TestIssueId, accountId, cancellationToken);
        return new() { Bug = ToIssue(bug), TestIssue = testIssue is null ? null : ToIssue(testIssue), TestRunId = runLink?.TestRunId, TestRunItemId = itemLink?.TestRunItemId };
    }

    private async Task<TestManagementOutcome> GetMutationAccessAsync(Guid workspaceId, Guid accountId, CancellationToken cancellationToken)
    {
        var member = await dbContext.Members.Include(x => x.Workspace).SingleOrDefaultAsync(x => x.TestWorkspaceId == workspaceId && x.AccountId == accountId && x.Status == TestWorkspaceMemberStatus.Active, cancellationToken);
        if (member is null) return TestManagementOutcome.NotFound;
        return member.Workspace.Status == TestAssetStatus.Active && member.Role is TestWorkspaceRole.Owner or TestWorkspaceRole.Manager
            ? TestManagementOutcome.Succeeded : TestManagementOutcome.Forbidden;
    }
    private Task<bool> HasReadAccessAsync(Guid workspaceId, Guid accountId, CancellationToken cancellationToken) => dbContext.Members.AnyAsync(x => x.TestWorkspaceId == workspaceId && x.AccountId == accountId && x.Status == TestWorkspaceMemberStatus.Active, cancellationToken);
    private Task<bool> IsLinkedProjectAsync(Guid workspaceId, Guid projectId, CancellationToken cancellationToken) => dbContext.WorkspaceProjects.AnyAsync(x => x.TestWorkspaceId == workspaceId && x.ProjectId == projectId, cancellationToken);
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions))));
    private static TestCaseSourceResponse ToCaseResponse(TestCaseSourceLink link, IssueDirectoryEntry issue) => new() { Id = link.Id, TestCaseId = link.TestCaseId, TestIssue = ToIssue(issue), CreatedAt = link.CreatedAt };
    private static TestTraceIssueResponse ToIssue(IssueDirectoryEntry issue) => new() { Id = issue.Id, ProjectId = issue.ProjectId, ProjectCode = issue.ProjectCode, IssueNo = issue.IssueNo, Key = issue.Key, Title = issue.Title, TypeCode = issue.TypeCode, StatusCode = issue.StatusCode };
    private sealed record Rejection(TestManagementOutcome Outcome, string? Code);
}
