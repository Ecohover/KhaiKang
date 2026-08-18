using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestRunBugLinkTests
{
    [Fact]
    public void Constructor_CapturesRunBugOriginAndInitialAuditMetadata()
    {
        var workspaceId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var bugIssueId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 8, 11, 5, 0, 0, TimeSpan.Zero);

        var link = new TestRunBugLink(
            Guid.NewGuid(), workspaceId, runId, projectId, bugIssueId, actorId, createdAt);

        Assert.Equal(workspaceId, link.TestWorkspaceId);
        Assert.Equal(runId, link.TestRunId);
        Assert.Equal(projectId, link.ProjectId);
        Assert.Equal(bugIssueId, link.BugIssueId);
        Assert.Equal(actorId, link.CreatedByAccountId);
        Assert.Equal(actorId, link.UpdatedByAccountId);
        Assert.Equal(createdAt, link.CreatedAt);
        Assert.Equal(createdAt, link.UpdatedAt);
        Assert.Equal(1, link.Version);
    }
}
