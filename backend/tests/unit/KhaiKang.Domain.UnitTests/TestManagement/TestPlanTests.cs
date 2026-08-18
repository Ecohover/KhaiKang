using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestPlanTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CapturesTestIssueAndStartsAsDraft()
    {
        var projectId = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var plan = new TestPlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Release plan",
            "Regression scope",
            actorId,
            CreatedAt,
            projectId,
            issueId);

        Assert.Equal(TestPlanStatus.Draft, plan.Status);
        Assert.Equal(projectId, plan.TestIssueProjectId);
        Assert.Equal(issueId, plan.TestIssueId);
        Assert.Equal(actorId, plan.CreatedByAccountId);
        Assert.Equal(actorId, plan.UpdatedByAccountId);
        Assert.Equal(1, plan.Version);
    }

    [Fact]
    public void Update_ReplacesEditableFieldsAndTestIssueReference()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var replacementProjectId = Guid.NewGuid();
        var replacementIssueId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var updatedAt = CreatedAt.AddHours(1);

        plan.Update(
            "Updated plan",
            "Updated scope",
            TestPlanStatus.Active,
            actorId,
            updatedAt,
            replacementProjectId,
            replacementIssueId);

        Assert.Equal("Updated plan", plan.Name);
        Assert.Equal("Updated scope", plan.Description);
        Assert.Equal(TestPlanStatus.Active, plan.Status);
        Assert.Equal(replacementProjectId, plan.TestIssueProjectId);
        Assert.Equal(replacementIssueId, plan.TestIssueId);
        Assert.Equal(actorId, plan.UpdatedByAccountId);
        Assert.Equal(updatedAt, plan.UpdatedAt);
        Assert.Equal(2, plan.Version);
    }

    [Fact]
    public void Update_CanClearTheOptionalTestIssueReference()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());

        plan.Update(
            plan.Name,
            plan.Description,
            plan.Status,
            Guid.NewGuid(),
            CreatedAt.AddHours(1));

        Assert.Null(plan.TestIssueProjectId);
        Assert.Null(plan.TestIssueId);
    }

    private static TestPlan CreatePlan(Guid? projectId = null, Guid? issueId = null)
    {
        return new TestPlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Release plan",
            null,
            Guid.NewGuid(),
            CreatedAt,
            projectId,
            issueId);
    }
}
