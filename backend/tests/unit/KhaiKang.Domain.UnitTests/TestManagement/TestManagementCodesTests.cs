using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestManagementCodesTests
{
    [Theory]
    [InlineData(TestAssetStatus.Active, "active")]
    [InlineData(TestAssetStatus.Inactive, "inactive")]
    public void AssetStatus_RoundTripsStableDatabaseCode(TestAssetStatus status, string code)
    {
        Assert.Equal(code, status.ToCode());
        Assert.Equal(status, TestManagementCodes.ParseAssetStatus(code));
    }

    [Theory]
    [InlineData(TestWorkspaceMemberStatus.Active, "active")]
    [InlineData(TestWorkspaceMemberStatus.Removed, "removed")]
    public void WorkspaceMemberStatus_RoundTripsStableDatabaseCode(
        TestWorkspaceMemberStatus status,
        string code)
    {
        Assert.Equal(code, status.ToCode());
        Assert.Equal(status, TestManagementCodes.ParseWorkspaceMemberStatus(code));
    }

    [Theory]
    [InlineData(TestWorkspaceRole.Owner, "owner")]
    [InlineData(TestWorkspaceRole.Manager, "manager")]
    [InlineData(TestWorkspaceRole.Tester, "tester")]
    [InlineData(TestWorkspaceRole.Viewer, "viewer")]
    public void WorkspaceRole_RoundTripsStableDatabaseCode(TestWorkspaceRole role, string code)
    {
        Assert.Equal(code, role.ToCode());
        Assert.Equal(role, TestManagementCodes.ParseWorkspaceRole(code));
    }

    [Theory]
    [InlineData(TestPlanStatus.Draft, "draft")]
    [InlineData(TestPlanStatus.Active, "active")]
    [InlineData(TestPlanStatus.Archived, "archived")]
    public void PlanStatus_RoundTripsStableDatabaseCode(TestPlanStatus status, string code)
    {
        Assert.Equal(code, status.ToCode());
        Assert.Equal(status, TestManagementCodes.ParsePlanStatus(code));
    }

    [Theory]
    [InlineData(TestRunStatus.NotStarted, "not_started")]
    [InlineData(TestRunStatus.InProgress, "in_progress")]
    [InlineData(TestRunStatus.Completed, "completed")]
    [InlineData(TestRunStatus.Cancelled, "cancelled")]
    public void RunStatus_RoundTripsStableDatabaseCode(TestRunStatus status, string code)
    {
        Assert.Equal(code, status.ToCode());
        Assert.Equal(status, TestManagementCodes.ParseRunStatus(code));
    }

    [Theory]
    [InlineData(TestResultStatus.NotRun, "not_run")]
    [InlineData(TestResultStatus.Passed, "passed")]
    [InlineData(TestResultStatus.Failed, "failed")]
    [InlineData(TestResultStatus.Blocked, "blocked")]
    [InlineData(TestResultStatus.Skipped, "skipped")]
    public void ResultStatus_RoundTripsStableDatabaseCode(TestResultStatus status, string code)
    {
        Assert.Equal(code, status.ToCode());
        Assert.Equal(status, TestManagementCodes.ParseResultStatus(code));
    }

    [Theory]
    [InlineData(TestNumberType.Case, "case")]
    [InlineData(TestNumberType.Plan, "plan")]
    [InlineData(TestNumberType.Run, "run")]
    public void NumberType_MapsToStableDatabaseCode(TestNumberType numberType, string code)
    {
        Assert.Equal(code, numberType.ToCode());
    }

    [Fact]
    public void ParseRunStatus_WhenDatabaseCodeIsUnknown_ThrowsClearException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => TestManagementCodes.ParseRunStatus("unknown"));

        Assert.Contains(nameof(TestRunStatus), exception.Message);
        Assert.Contains("unknown", exception.Message);
    }
}
