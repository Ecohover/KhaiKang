using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestRunTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CapturesPlanTestIssueSnapshotAndInitialState()
    {
        var projectId = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var run = CreateRun(projectId, issueId, actorId);

        Assert.Equal("not_started", run.Status);
        Assert.Equal(projectId, run.TestIssueProjectId);
        Assert.Equal(issueId, run.TestIssueId);
        Assert.Equal(actorId, run.StartedByAccountId);
        Assert.Null(run.StartedAt);
        Assert.Null(run.CompletedAt);
        Assert.Equal(1, run.Version);
    }

    [Fact]
    public void PlanUpdate_DoesNotChangeExistingRunTestIssueSnapshot()
    {
        var originalProjectId = Guid.NewGuid();
        var originalIssueId = Guid.NewGuid();
        var plan = new TestPlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Plan",
            null,
            Guid.NewGuid(),
            CreatedAt,
            originalProjectId,
            originalIssueId);
        var run = new TestRun(
            Guid.NewGuid(),
            plan.Id,
            1,
            "Run",
            Guid.NewGuid(),
            CreatedAt,
            plan.TestIssueProjectId,
            plan.TestIssueId);

        plan.Update(
            plan.Name,
            plan.Description,
            plan.Status,
            Guid.NewGuid(),
            CreatedAt.AddHours(1),
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.Equal(originalProjectId, run.TestIssueProjectId);
        Assert.Equal(originalIssueId, run.TestIssueId);
    }

    [Fact]
    public void MarkInProgress_FromNotStarted_StartsTheRun()
    {
        var run = CreateRun();
        var actorId = Guid.NewGuid();
        var startedAt = CreatedAt.AddMinutes(10);

        run.MarkInProgress(actorId, startedAt);

        Assert.Equal("in_progress", run.Status);
        Assert.Equal(startedAt, run.StartedAt);
        Assert.Null(run.CompletedAt);
        Assert.Equal(actorId, run.UpdatedByAccountId);
        Assert.Equal(2, run.Version);
    }

    [Fact]
    public void MarkInProgress_FromCompleted_DoesNotChangeTheRun()
    {
        var run = CreateRun();
        var completedAt = CreatedAt.AddHours(1);
        run.Finish("completed", "Done", Guid.NewGuid(), completedAt);
        var version = run.Version;

        run.MarkInProgress(Guid.NewGuid(), completedAt.AddHours(1));

        Assert.Equal("completed", run.Status);
        Assert.Equal(completedAt, run.StartedAt);
        Assert.Equal(completedAt, run.CompletedAt);
        Assert.Equal(version, run.Version);
    }

    [Fact]
    public void Finish_AfterStart_PreservesStartedAtAndCapturesCompletion()
    {
        var run = CreateRun();
        var startedAt = CreatedAt.AddMinutes(10);
        var completedAt = CreatedAt.AddHours(1);
        run.MarkInProgress(Guid.NewGuid(), startedAt);
        var actorId = Guid.NewGuid();

        run.Finish("completed", "All passed", actorId, completedAt);

        Assert.Equal("completed", run.Status);
        Assert.Equal("All passed", run.Summary);
        Assert.Equal(startedAt, run.StartedAt);
        Assert.Equal(completedAt, run.CompletedAt);
        Assert.Equal(actorId, run.UpdatedByAccountId);
        Assert.Equal(3, run.Version);
    }

    private static TestRun CreateRun(
        Guid? projectId = null,
        Guid? issueId = null,
        Guid? actorId = null)
    {
        return new TestRun(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Run",
            actorId ?? Guid.NewGuid(),
            CreatedAt,
            projectId,
            issueId);
    }
}
