namespace KhaiKang.Modules.TestManagement.Domain;

public static class TestManagementCodes
{
    private const string AssetActiveCode = "active";
    private const string AssetInactiveCode = "inactive";
    private const string WorkspaceMemberActiveCode = "active";
    private const string WorkspaceMemberRemovedCode = "removed";
    private const string WorkspaceRoleOwnerCode = "owner";
    private const string WorkspaceRoleManagerCode = "manager";
    private const string WorkspaceRoleTesterCode = "tester";
    private const string WorkspaceRoleViewerCode = "viewer";
    private const string PlanDraftCode = "draft";
    private const string PlanActiveCode = "active";
    private const string PlanArchivedCode = "archived";
    private const string RunNotStartedCode = "not_started";
    private const string RunInProgressCode = "in_progress";
    private const string RunCompletedCode = "completed";
    private const string RunCancelledCode = "cancelled";
    private const string ResultNotRunCode = "not_run";
    private const string ResultPassedCode = "passed";
    private const string ResultFailedCode = "failed";
    private const string ResultBlockedCode = "blocked";
    private const string ResultSkippedCode = "skipped";
    private const string CaseNumberCode = "case";
    private const string PlanNumberCode = "plan";
    private const string RunNumberCode = "run";

    public static string ToCode(this TestAssetStatus status) => status switch
    {
        TestAssetStatus.Active => AssetActiveCode,
        TestAssetStatus.Inactive => AssetInactiveCode,
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestWorkspaceMemberStatus status) => status switch
    {
        TestWorkspaceMemberStatus.Active => WorkspaceMemberActiveCode,
        TestWorkspaceMemberStatus.Removed => WorkspaceMemberRemovedCode,
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestWorkspaceRole role) => role switch
    {
        TestWorkspaceRole.Owner => WorkspaceRoleOwnerCode,
        TestWorkspaceRole.Manager => WorkspaceRoleManagerCode,
        TestWorkspaceRole.Tester => WorkspaceRoleTesterCode,
        TestWorkspaceRole.Viewer => WorkspaceRoleViewerCode,
        _ => throw UnknownValue(role),
    };

    public static string ToCode(this TestPlanStatus status) => status switch
    {
        TestPlanStatus.Draft => PlanDraftCode,
        TestPlanStatus.Active => PlanActiveCode,
        TestPlanStatus.Archived => PlanArchivedCode,
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestRunStatus status) => status switch
    {
        TestRunStatus.NotStarted => RunNotStartedCode,
        TestRunStatus.InProgress => RunInProgressCode,
        TestRunStatus.Completed => RunCompletedCode,
        TestRunStatus.Cancelled => RunCancelledCode,
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestResultStatus status) => status switch
    {
        TestResultStatus.NotRun => ResultNotRunCode,
        TestResultStatus.Passed => ResultPassedCode,
        TestResultStatus.Failed => ResultFailedCode,
        TestResultStatus.Blocked => ResultBlockedCode,
        TestResultStatus.Skipped => ResultSkippedCode,
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestNumberType numberType) => numberType switch
    {
        TestNumberType.Case => CaseNumberCode,
        TestNumberType.Plan => PlanNumberCode,
        TestNumberType.Run => RunNumberCode,
        _ => throw UnknownValue(numberType),
    };

    public static bool TryParseAssetStatus(string? code, out TestAssetStatus status)
    {
        if (CodeEquals(code, AssetActiveCode))
        {
            status = TestAssetStatus.Active;
            return true;
        }

        if (CodeEquals(code, AssetInactiveCode))
        {
            status = TestAssetStatus.Inactive;
            return true;
        }

        status = default;
        return false;
    }

    public static bool TryParseWorkspaceMemberStatus(
        string? code,
        out TestWorkspaceMemberStatus status)
    {
        if (CodeEquals(code, WorkspaceMemberActiveCode))
        {
            status = TestWorkspaceMemberStatus.Active;
            return true;
        }

        if (CodeEquals(code, WorkspaceMemberRemovedCode))
        {
            status = TestWorkspaceMemberStatus.Removed;
            return true;
        }

        status = default;
        return false;
    }

    public static bool TryParseWorkspaceRole(string? code, out TestWorkspaceRole role)
    {
        if (CodeEquals(code, WorkspaceRoleOwnerCode))
        {
            role = TestWorkspaceRole.Owner;
            return true;
        }

        if (CodeEquals(code, WorkspaceRoleManagerCode))
        {
            role = TestWorkspaceRole.Manager;
            return true;
        }

        if (CodeEquals(code, WorkspaceRoleTesterCode))
        {
            role = TestWorkspaceRole.Tester;
            return true;
        }

        if (CodeEquals(code, WorkspaceRoleViewerCode))
        {
            role = TestWorkspaceRole.Viewer;
            return true;
        }

        role = default;
        return false;
    }

    public static bool TryParsePlanStatus(string? code, out TestPlanStatus status)
    {
        if (CodeEquals(code, PlanDraftCode))
        {
            status = TestPlanStatus.Draft;
            return true;
        }

        if (CodeEquals(code, PlanActiveCode))
        {
            status = TestPlanStatus.Active;
            return true;
        }

        if (CodeEquals(code, PlanArchivedCode))
        {
            status = TestPlanStatus.Archived;
            return true;
        }

        status = default;
        return false;
    }

    public static bool TryParseRunStatus(string? code, out TestRunStatus status)
    {
        if (CodeEquals(code, RunNotStartedCode))
        {
            status = TestRunStatus.NotStarted;
            return true;
        }

        if (CodeEquals(code, RunInProgressCode))
        {
            status = TestRunStatus.InProgress;
            return true;
        }

        if (CodeEquals(code, RunCompletedCode))
        {
            status = TestRunStatus.Completed;
            return true;
        }

        if (CodeEquals(code, RunCancelledCode))
        {
            status = TestRunStatus.Cancelled;
            return true;
        }

        status = default;
        return false;
    }

    public static bool TryParseResultStatus(string? code, out TestResultStatus status)
    {
        if (CodeEquals(code, ResultNotRunCode))
        {
            status = TestResultStatus.NotRun;
            return true;
        }

        if (CodeEquals(code, ResultPassedCode))
        {
            status = TestResultStatus.Passed;
            return true;
        }

        if (CodeEquals(code, ResultFailedCode))
        {
            status = TestResultStatus.Failed;
            return true;
        }

        if (CodeEquals(code, ResultBlockedCode))
        {
            status = TestResultStatus.Blocked;
            return true;
        }

        if (CodeEquals(code, ResultSkippedCode))
        {
            status = TestResultStatus.Skipped;
            return true;
        }

        status = default;
        return false;
    }

    public static TestAssetStatus ParseAssetStatus(string code)
    {
        if (TryParseAssetStatus(code, out var status))
        {
            return status;
        }

        throw UnsupportedDatabaseValue<TestAssetStatus>(code);
    }

    public static TestWorkspaceMemberStatus ParseWorkspaceMemberStatus(string code)
    {
        if (TryParseWorkspaceMemberStatus(code, out var status))
        {
            return status;
        }

        throw UnsupportedDatabaseValue<TestWorkspaceMemberStatus>(code);
    }

    public static TestWorkspaceRole ParseWorkspaceRole(string code)
    {
        if (TryParseWorkspaceRole(code, out var role))
        {
            return role;
        }

        throw UnsupportedDatabaseValue<TestWorkspaceRole>(code);
    }

    public static TestPlanStatus ParsePlanStatus(string code)
    {
        if (TryParsePlanStatus(code, out var status))
        {
            return status;
        }

        throw UnsupportedDatabaseValue<TestPlanStatus>(code);
    }

    public static TestRunStatus ParseRunStatus(string code)
    {
        if (TryParseRunStatus(code, out var status))
        {
            return status;
        }

        throw UnsupportedDatabaseValue<TestRunStatus>(code);
    }

    public static TestResultStatus ParseResultStatus(string code)
    {
        if (TryParseResultStatus(code, out var status))
        {
            return status;
        }

        throw UnsupportedDatabaseValue<TestResultStatus>(code);
    }

    private static InvalidOperationException UnsupportedDatabaseValue<T>(string code)
        where T : struct, Enum
        => new($"Unsupported {typeof(T).Name} database value '{code}'.");

    private static bool CodeEquals(string? actualCode, string expectedCode) =>
        string.Equals(actualCode, expectedCode, StringComparison.OrdinalIgnoreCase);

    private static ArgumentOutOfRangeException UnknownValue<T>(T value)
        where T : struct, Enum =>
        new(nameof(value), value, $"Unsupported {typeof(T).Name} value.");

}
