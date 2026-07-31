using System.Text.RegularExpressions;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;
using KhaiKang.Modules.TestManagement.Domain;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KhaiKang.Modules.TestManagement.Application;

public enum TestManagementOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    Conflict,
    Invalid,
}

public sealed record TestManagementResult<T>(
    TestManagementOutcome Outcome,
    T? Value = default,
    string? Code = null);

public sealed class TestManagementService(
    TestManagementDbContext dbContext,
    IAccountDirectory accountDirectory,
    TimeProvider timeProvider)
{
    private static readonly string[] Roles = ["owner", "manager", "tester", "viewer"];

    public async Task<IReadOnlyList<TestWorkspaceResponse>> ListWorkspacesAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Members.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Status == "active")
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
                x.AccountId == accountId && x.Status == "active")
            .Select(x => ToWorkspaceResponse(x.Workspace, x.Role))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TestManagementResult<TestWorkspaceResponse>> CreateWorkspaceAsync(
        Guid accountId,
        CreateTestWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await dbContext.Workspaces.AnyAsync(x => x.Name == name, cancellationToken))
        {
            return new(TestManagementOutcome.Conflict, Code: "workspace_name_conflict");
        }

        var prefix = string.IsNullOrWhiteSpace(request.Prefix)
            ? await GeneratePrefixAsync(name, cancellationToken)
            : request.Prefix.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(prefix, "^[A-Z][A-Z0-9]{1,9}$"))
        {
            return new(TestManagementOutcome.Invalid, Code: "workspace_prefix_invalid");
        }
        if (await dbContext.Workspaces.AnyAsync(x => x.Prefix == prefix, cancellationToken))
        {
            return new(TestManagementOutcome.Conflict, Code: "workspace_prefix_conflict");
        }

        var now = timeProvider.GetUtcNow();
        var workspace = new TestWorkspace(
            Guid.NewGuid(), name, prefix, Clean(request.Description), accountId, now);
        var owner = new TestWorkspaceMember(
            Guid.NewGuid(), workspace.Id, accountId, "owner", accountId, now);
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
            return new(TestManagementOutcome.Conflict, Code: "workspace_prefix_conflict");
        }
        return new(TestManagementOutcome.Succeeded, ToWorkspaceResponse(workspace, "owner"));
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
            return new(TestManagementOutcome.NotFound);
        }

        if (access.Role != "owner")
        {
            return new(TestManagementOutcome.Forbidden);
        }

        var workspace = access.Workspace;
        if (workspace.Version != request.Version)
        {
            return new(TestManagementOutcome.Conflict, Code: "workspace_version_conflict");
        }

        var name = request.Name.Trim();
        if (await dbContext.Workspaces.AnyAsync(
            x => x.Id != workspaceId && x.Name == name, cancellationToken))
        {
            return new(TestManagementOutcome.Conflict, Code: "workspace_name_conflict");
        }

        workspace.Update(name, Clean(request.Description), request.Status, accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(TestManagementOutcome.Succeeded, ToWorkspaceResponse(workspace, access.Role));
    }

    public async Task<TestManagementResult<IReadOnlyList<TestWorkspaceMemberResponse>>> ListMembersAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return new(TestManagementOutcome.NotFound);
        }

        var members = await dbContext.Members.AsNoTracking()
            .Where(x => x.TestWorkspaceId == workspaceId && x.Status == "active")
            .OrderBy(x => x.JoinedAt).ToListAsync(cancellationToken);
        var accounts = await accountDirectory.GetByIdsAsync(
            members.Select(x => x.AccountId).ToArray(), cancellationToken);
        return new(TestManagementOutcome.Succeeded, members.Select(x =>
            new TestWorkspaceMemberResponse(
                x.Id, x.AccountId,
                accounts.GetValueOrDefault(x.AccountId)?.Username ?? x.AccountId.ToString(),
                x.Role, x.Status, x.JoinedAt, x.Version)).ToArray());
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
            return new(TestManagementOutcome.NotFound);
        }

        if (!CanManage(access.Role) || !Roles.Contains(request.Role))
        {
            return new(access.Role is "owner" or "manager"
                ? TestManagementOutcome.Invalid : TestManagementOutcome.Forbidden);
        }

        var account = await accountDirectory.FindActiveByUsernameAsync(
            request.Username.Trim(), cancellationToken);
        if (account is null)
        {
            return new(TestManagementOutcome.NotFound, Code: "account_not_found");
        }

        var member = await dbContext.Members.SingleOrDefaultAsync(
            x => x.TestWorkspaceId == workspaceId && x.AccountId == account.Id,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (member?.Status == "active")
        {
            return new(TestManagementOutcome.Conflict, Code: "member_already_active");
        }

        if (member is null)
        {
            member = new TestWorkspaceMember(
                Guid.NewGuid(), workspaceId, account.Id, request.Role, accountId, now);
            dbContext.Members.Add(member);
        }
        else
        {
            member.Restore(request.Role, accountId, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new(TestManagementOutcome.Succeeded, new(
            member.Id, member.AccountId, account.Username, member.Role,
            member.Status, member.JoinedAt, member.Version));
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
            return new(TestManagementOutcome.NotFound);
        }

        if (!CanManage(access.Role))
        {
            return new(TestManagementOutcome.Forbidden);
        }

        var member = await dbContext.Members.SingleOrDefaultAsync(
            x => x.Id == memberId && x.TestWorkspaceId == workspaceId && x.Status == "active",
            cancellationToken);
        if (member is null)
        {
            return new(TestManagementOutcome.NotFound);
        }

        if (!Roles.Contains(request.Role))
        {
            return new(TestManagementOutcome.Invalid);
        }

        if (member.Version != request.Version)
        {
            return new(TestManagementOutcome.Conflict, Code: "member_version_conflict");
        }

        if (member.Role == "owner" && request.Role != "owner" &&
            await ActiveOwnerCountAsync(workspaceId, cancellationToken) <= 1)
        {
            return new(TestManagementOutcome.Conflict, Code: "last_owner");
        }

        member.ChangeRole(request.Role, accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        var accounts = await accountDirectory.GetByIdsAsync([member.AccountId], cancellationToken);
        return new(TestManagementOutcome.Succeeded, new(
            member.Id, member.AccountId,
            accounts.GetValueOrDefault(member.AccountId)?.Username ?? member.AccountId.ToString(),
            member.Role, member.Status, member.JoinedAt, member.Version));
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
            return new(TestManagementOutcome.NotFound);
        }

        if (!CanManage(access.Role))
        {
            return new(TestManagementOutcome.Forbidden);
        }

        var member = await dbContext.Members.SingleOrDefaultAsync(
            x => x.Id == memberId && x.TestWorkspaceId == workspaceId && x.Status == "active",
            cancellationToken);
        if (member is null)
        {
            return new(TestManagementOutcome.NotFound);
        }

        if (member.Version != version)
        {
            return new(TestManagementOutcome.Conflict, Code: "member_version_conflict");
        }

        if (member.Role == "owner" &&
            await ActiveOwnerCountAsync(workspaceId, cancellationToken) <= 1)
        {
            return new(TestManagementOutcome.Conflict, Code: "last_owner");
        }

        member.Remove(accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(TestManagementOutcome.Succeeded, new object());
    }

    public async Task<TestManagementResult<IReadOnlyList<TestSuiteResponse>>> ListSuitesAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return new(TestManagementOutcome.NotFound);
        }

        var suites = await dbContext.Suites.AsNoTracking()
            .Where(x => x.TestWorkspaceId == workspaceId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return new(TestManagementOutcome.Succeeded, MapSuites(suites));
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
            return new(TestManagementOutcome.NotFound);
        }

        if (!CanManageAssets(access.Role) || access.Workspace.Status != "active")
        {
            return new(TestManagementOutcome.Forbidden);
        }

        var depth = await ParentDepthAsync(workspaceId, request.ParentId, cancellationToken);
        if (depth is null || depth >= 5)
        {
            return new(TestManagementOutcome.Invalid, Code: "invalid_parent");
        }

        if (await HasSuiteNameAsync(workspaceId, request.ParentId, request.Name, null, cancellationToken))
        {
            return new(TestManagementOutcome.Conflict, Code: "suite_name_conflict");
        }

        var suite = new TestSuite(
            Guid.NewGuid(), workspaceId, request.ParentId, request.Name.Trim(),
            Clean(request.Description), request.SortOrder, accountId, timeProvider.GetUtcNow());
        dbContext.Suites.Add(suite);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(TestManagementOutcome.Succeeded, ToSuiteResponse(suite, depth.Value + 1));
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
            return new(TestManagementOutcome.NotFound);
        }

        if (!CanManageAssets(access.Role) || access.Workspace.Status != "active")
        {
            return new(TestManagementOutcome.Forbidden);
        }

        var suite = await dbContext.Suites.SingleOrDefaultAsync(
            x => x.Id == suiteId && x.TestWorkspaceId == workspaceId, cancellationToken);
        if (suite is null)
        {
            return new(TestManagementOutcome.NotFound);
        }

        if (suite.Version != request.Version)
        {
            return new(TestManagementOutcome.Conflict, Code: "suite_version_conflict");
        }

        if (request.ParentId == suiteId)
        {
            return new(TestManagementOutcome.Invalid, Code: "invalid_parent");
        }

        var allSuites = await dbContext.Suites.AsNoTracking()
            .Where(x => x.TestWorkspaceId == workspaceId).ToListAsync(cancellationToken);
        if (DescendantIds(suiteId, allSuites).Contains(request.ParentId ?? Guid.Empty))
        {
            return new(TestManagementOutcome.Invalid, Code: "suite_cycle");
        }

        var parentDepth = DepthOf(request.ParentId, allSuites);
        var subtreeHeight = HeightOf(suiteId, allSuites);
        if (parentDepth is null || parentDepth.Value + subtreeHeight > 5)
        {
            return new(TestManagementOutcome.Invalid, Code: "suite_depth");
        }

        if (await HasSuiteNameAsync(
            workspaceId, request.ParentId, request.Name, suiteId, cancellationToken))
        {
            return new(TestManagementOutcome.Conflict, Code: "suite_name_conflict");
        }

        suite.Update(
            request.ParentId, request.Name.Trim(), Clean(request.Description),
            request.SortOrder, request.Status, accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(TestManagementOutcome.Succeeded, ToSuiteResponse(suite, parentDepth.Value + 1));
    }

    public async Task<TestManagementResult<IReadOnlyList<TestCaseResponse>>> ListCasesAsync(
        Guid workspaceId,
        Guid accountId,
        Guid? suiteId,
        CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return new(TestManagementOutcome.NotFound);
        }

        var query = dbContext.Cases.AsNoTracking()
            .Where(x => x.Suite.TestWorkspaceId == workspaceId);
        if (suiteId is not null)
        {
            query = query.Where(x => x.TestSuiteId == suiteId);
        }

        var cases = await query
            .Include(x => x.Steps)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);
        return new(TestManagementOutcome.Succeeded, cases.Select(ToCaseResponse).ToArray());
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
            return new(TestManagementOutcome.NotFound);
        }

        if (!CanManageAssets(access.Role) || access.Workspace.Status != "active")
        {
            return new(TestManagementOutcome.Forbidden);
        }

        var suite = await dbContext.Suites.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == request.SuiteId && x.TestWorkspaceId == workspaceId,
            cancellationToken);
        if (suite is null || suite.Status != "active")
        {
            return new(TestManagementOutcome.NotFound, Code: "test_suite_not_found");
        }

        var now = timeProvider.GetUtcNow();
        var testCase = new TestCase(
            Guid.NewGuid(),
            suite.Id,
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
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(TestManagementOutcome.Succeeded, ToCaseResponse(testCase));
    }

    public async Task<TestManagementResult<TestCaseResponse>> GetCaseAsync(
        Guid workspaceId,
        Guid caseId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await AccessAsync(workspaceId, accountId, cancellationToken) is null)
        {
            return new(TestManagementOutcome.NotFound);
        }

        var testCase = await dbContext.Cases.AsNoTracking()
            .Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == caseId && x.Suite.TestWorkspaceId == workspaceId, cancellationToken);
        return testCase is null
            ? new(TestManagementOutcome.NotFound)
            : new(TestManagementOutcome.Succeeded, ToCaseResponse(testCase));
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
            return new(TestManagementOutcome.NotFound);
        }

        if (!CanManageAssets(access.Role) || access.Workspace.Status != "active")
        {
            return new(TestManagementOutcome.Forbidden);
        }

        var testCase = await dbContext.Cases
            .Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == caseId && x.Suite.TestWorkspaceId == workspaceId, cancellationToken);
        if (testCase is null)
        {
            return new(TestManagementOutcome.NotFound);
        }

        if (testCase.Version != request.Version)
        {
            return new(TestManagementOutcome.Conflict, Code: "case_version_conflict");
        }

        var targetSuite = await dbContext.Suites.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == request.SuiteId && x.TestWorkspaceId == workspaceId,
            cancellationToken);
        if (targetSuite is null || targetSuite.Status != "active")
        {
            return new(TestManagementOutcome.Conflict, Code: "test_suite_not_found");
        }

        var now = timeProvider.GetUtcNow();
        testCase.Update(
            targetSuite.Id,
            request.Title.Trim(),
            Clean(request.Description),
            Clean(request.Preconditions),
            Clean(request.OverallExpectedResult),
            request.SortOrder,
            request.Status,
            accountId,
            now);

        foreach (var step in testCase.Steps.ToList())
        {
            dbContext.CaseSteps.Remove(step);
        }
        testCase.ClearSteps();
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

        await dbContext.SaveChangesAsync(cancellationToken);
        return new(TestManagementOutcome.Succeeded, ToCaseResponse(testCase));
    }

    private async Task<TestWorkspaceMember?> AccessAsync(
        Guid workspaceId, Guid accountId, CancellationToken cancellationToken) =>
        await dbContext.Members.Include(x => x.Workspace).SingleOrDefaultAsync(
            x => x.TestWorkspaceId == workspaceId && x.AccountId == accountId &&
                x.Status == "active", cancellationToken);

    private async Task<int> ActiveOwnerCountAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        await dbContext.Members.CountAsync(
            x => x.TestWorkspaceId == workspaceId && x.Status == "active" && x.Role == "owner",
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

    private static bool CanManage(string role) => role is "owner" or "manager";
    private static bool CanManageAssets(string role) => role is "owner" or "manager";
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
    private static TestWorkspaceResponse ToWorkspaceResponse(TestWorkspace x, string role) =>
        new(x.Id, x.Name, x.Prefix, x.Description, x.Status, role, x.CreatedAt, x.UpdatedAt, x.Version);
    private static TestSuiteResponse ToSuiteResponse(TestSuite x, int depth) =>
        new(x.Id, x.ParentId, x.Name, x.Description, x.SortOrder, x.Status, depth, x.Version);
    private static TestCaseResponse ToCaseResponse(TestCase x) =>
        new(
            x.Id,
            x.TestSuiteId,
            x.Title,
            x.Description,
            x.Preconditions,
            x.OverallExpectedResult,
            x.SortOrder,
            x.Status,
            x.Steps.OrderBy(step => step.StepNo)
                .Select(step => new TestCaseStepResponse(
                    step.Id,
                    step.StepNo,
                    step.Action,
                    step.ExpectedResult))
                .ToArray(),
            x.CreatedAt,
            x.UpdatedAt,
            x.Version);
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
