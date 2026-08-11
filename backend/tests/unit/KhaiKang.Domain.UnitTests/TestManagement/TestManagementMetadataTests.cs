using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestManagementMetadataTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WorkspaceProject_CapturesBothResourceIdentifiersAndAuditMetadata()
    {
        var workspaceId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var link = new TestWorkspaceProject(
            Guid.NewGuid(), workspaceId, projectId, actorId, CreatedAt);

        Assert.Equal(workspaceId, link.TestWorkspaceId);
        Assert.Equal(projectId, link.ProjectId);
        Assert.Equal(actorId, link.CreatedByAccountId);
        Assert.Equal(1, link.Version);
    }

    [Fact]
    public void CaseTag_CapturesBothResourceIdentifiersAndAuditMetadata()
    {
        var caseId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var link = new TestCaseTag(Guid.NewGuid(), caseId, tagId, actorId, CreatedAt);

        Assert.Equal(caseId, link.TestCaseId);
        Assert.Equal(tagId, link.TestTagId);
        Assert.Equal(actorId, link.CreatedByAccountId);
        Assert.Equal(1, link.Version);
    }

    [Fact]
    public void PlanItem_CapturesCaseOrderAndAuditMetadata()
    {
        var planId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var item = new TestPlanItem(
            Guid.NewGuid(), planId, caseId, 4, actorId, CreatedAt);

        Assert.Equal(planId, item.TestPlanId);
        Assert.Equal(caseId, item.TestCaseId);
        Assert.Equal(4, item.SortOrder);
        Assert.Equal(actorId, item.CreatedByAccountId);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void Step_CapturesOrderedInstructionsAndAuditMetadata()
    {
        var caseId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var step = new TestStep(
            Guid.NewGuid(), caseId, 2, "Action", "Expected", actorId, CreatedAt);

        Assert.Equal(caseId, step.TestCaseId);
        Assert.Equal(2, step.StepNo);
        Assert.Equal("Action", step.Action);
        Assert.Equal("Expected", step.ExpectedResult);
        Assert.Equal(actorId, step.CreatedByAccountId);
        Assert.Equal(1, step.Version);
    }
}
