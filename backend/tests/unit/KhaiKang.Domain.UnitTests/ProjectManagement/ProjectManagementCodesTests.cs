using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class ProjectManagementCodesTests
{
    [Theory]
    [InlineData(ProjectStatus.Active, "active")]
    [InlineData(ProjectStatus.Inactive, "inactive")]
    public void ProjectStatus_RoundTripsStableDatabaseCode(ProjectStatus status, string code)
    {
        Assert.Equal(code, status.ToCode());
        Assert.Equal(status, ProjectManagementCodes.ParseProjectStatus(code));
        Assert.True(ProjectManagementCodes.IsProjectStatusCode(code));
        Assert.True(ProjectManagementCodes.IsProjectStatusCode(code.ToUpperInvariant()));
    }

    [Fact]
    public void IsProjectStatusCode_RejectsUnknownCode()
    {
        Assert.False(ProjectManagementCodes.IsProjectStatusCode("archived"));
    }

    [Theory]
    [InlineData(ProjectMemberStatus.Active, "active")]
    [InlineData(ProjectMemberStatus.Removed, "removed")]
    public void ProjectMemberStatus_RoundTripsStableDatabaseCode(
        ProjectMemberStatus status,
        string code)
    {
        Assert.Equal(code, status.ToCode());
        Assert.Equal(status, ProjectManagementCodes.ParseProjectMemberStatus(code));
    }

    [Theory]
    [InlineData(IssueStatusCategory.Todo, "todo")]
    [InlineData(IssueStatusCategory.Doing, "doing")]
    [InlineData(IssueStatusCategory.Done, "done")]
    public void IssueStatusCategory_RoundTripsStableDatabaseCode(
        IssueStatusCategory category,
        string code)
    {
        Assert.Equal(code, category.ToCode());
        Assert.Equal(category, ProjectManagementCodes.ParseIssueStatusCategory(code));
    }
}
