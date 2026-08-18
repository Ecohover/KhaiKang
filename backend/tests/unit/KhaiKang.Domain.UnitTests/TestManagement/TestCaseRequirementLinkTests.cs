using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestCaseRequirementLinkTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CapturesTraceabilityAndInitialAuditMetadata()
    {
        var workspaceId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var link = new TestCaseRequirementLink(
            Guid.NewGuid(), workspaceId, caseId, projectId, issueId, actorId, CreatedAt);

        Assert.Equal(workspaceId, link.TestWorkspaceId);
        Assert.Equal(caseId, link.TestCaseId);
        Assert.Equal(projectId, link.ProjectId);
        Assert.Equal(issueId, link.RequirementIssueId);
        Assert.False(link.IsDeleted);
        Assert.Equal(actorId, link.CreatedByAccountId);
        Assert.Equal(actorId, link.UpdatedByAccountId);
        Assert.Equal(1, link.Version);
    }

    [Fact]
    public void Delete_PreservesLinkHistoryAndUpdatesVersion()
    {
        var link = CreateLink();
        var actorId = Guid.NewGuid();
        var deletedAt = CreatedAt.AddMinutes(30);

        link.Delete(actorId, deletedAt);

        Assert.True(link.IsDeleted);
        Assert.Equal(deletedAt, link.DeletedAt);
        Assert.Equal(actorId, link.DeletedByAccountId);
        Assert.Equal(actorId, link.UpdatedByAccountId);
        Assert.Equal(deletedAt, link.UpdatedAt);
        Assert.Equal(2, link.Version);
    }

    private static TestCaseRequirementLink CreateLink()
    {
        return new TestCaseRequirementLink(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAt);
    }
}
