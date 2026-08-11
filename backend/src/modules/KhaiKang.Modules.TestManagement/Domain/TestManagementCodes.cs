namespace KhaiKang.Modules.TestManagement.Domain;

public static class TestManagementCodes
{
    public static string ToCode(this TestAssetStatus status) => status switch
    {
        TestAssetStatus.Active => "active",
        TestAssetStatus.Inactive => "inactive",
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestWorkspaceMemberStatus status) => status switch
    {
        TestWorkspaceMemberStatus.Active => "active",
        TestWorkspaceMemberStatus.Removed => "removed",
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestWorkspaceRole role) => role switch
    {
        TestWorkspaceRole.Owner => "owner",
        TestWorkspaceRole.Manager => "manager",
        TestWorkspaceRole.Tester => "tester",
        TestWorkspaceRole.Viewer => "viewer",
        _ => throw UnknownValue(role),
    };

    public static string ToCode(this TestPlanStatus status) => status switch
    {
        TestPlanStatus.Draft => "draft",
        TestPlanStatus.Active => "active",
        TestPlanStatus.Archived => "archived",
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestRunStatus status) => status switch
    {
        TestRunStatus.NotStarted => "not_started",
        TestRunStatus.InProgress => "in_progress",
        TestRunStatus.Completed => "completed",
        TestRunStatus.Cancelled => "cancelled",
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestResultStatus status) => status switch
    {
        TestResultStatus.NotRun => "not_run",
        TestResultStatus.Passed => "passed",
        TestResultStatus.Failed => "failed",
        TestResultStatus.Blocked => "blocked",
        TestResultStatus.Skipped => "skipped",
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this TestNumberType numberType) => numberType switch
    {
        TestNumberType.Case => "case",
        TestNumberType.Plan => "plan",
        TestNumberType.Run => "run",
        _ => throw UnknownValue(numberType),
    };

    public static bool TryParseAssetStatus(string? code, out TestAssetStatus status)
    {
        if (CodeEquals(code, "active"))
        {
            status = TestAssetStatus.Active;
            return true;
        }

        if (CodeEquals(code, "inactive"))
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
        if (CodeEquals(code, "active"))
        {
            status = TestWorkspaceMemberStatus.Active;
            return true;
        }

        if (CodeEquals(code, "removed"))
        {
            status = TestWorkspaceMemberStatus.Removed;
            return true;
        }

        status = default;
        return false;
    }

    public static bool TryParseWorkspaceRole(string? code, out TestWorkspaceRole role)
    {
        if (CodeEquals(code, "owner"))
        {
            role = TestWorkspaceRole.Owner;
            return true;
        }

        if (CodeEquals(code, "manager"))
        {
            role = TestWorkspaceRole.Manager;
            return true;
        }

        if (CodeEquals(code, "tester"))
        {
            role = TestWorkspaceRole.Tester;
            return true;
        }

        if (CodeEquals(code, "viewer"))
        {
            role = TestWorkspaceRole.Viewer;
            return true;
        }

        role = default;
        return false;
    }

    public static bool TryParsePlanStatus(string? code, out TestPlanStatus status)
    {
        if (CodeEquals(code, "draft"))
        {
            status = TestPlanStatus.Draft;
            return true;
        }

        if (CodeEquals(code, "active"))
        {
            status = TestPlanStatus.Active;
            return true;
        }

        if (CodeEquals(code, "archived"))
        {
            status = TestPlanStatus.Archived;
            return true;
        }

        status = default;
        return false;
    }

    public static bool TryParseRunStatus(string? code, out TestRunStatus status)
    {
        if (CodeEquals(code, "not_started"))
        {
            status = TestRunStatus.NotStarted;
            return true;
        }

        if (CodeEquals(code, "in_progress"))
        {
            status = TestRunStatus.InProgress;
            return true;
        }

        if (CodeEquals(code, "completed"))
        {
            status = TestRunStatus.Completed;
            return true;
        }

        if (CodeEquals(code, "cancelled"))
        {
            status = TestRunStatus.Cancelled;
            return true;
        }

        status = default;
        return false;
    }

    public static bool TryParseResultStatus(string? code, out TestResultStatus status)
    {
        if (CodeEquals(code, "not_run"))
        {
            status = TestResultStatus.NotRun;
            return true;
        }

        if (CodeEquals(code, "passed"))
        {
            status = TestResultStatus.Passed;
            return true;
        }

        if (CodeEquals(code, "failed"))
        {
            status = TestResultStatus.Failed;
            return true;
        }

        if (CodeEquals(code, "blocked"))
        {
            status = TestResultStatus.Blocked;
            return true;
        }

        if (CodeEquals(code, "skipped"))
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
