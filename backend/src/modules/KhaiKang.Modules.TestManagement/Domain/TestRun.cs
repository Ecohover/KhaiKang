namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestRun
{
    private TestRun() { }

    public TestRun(
        Guid id,
        Guid planId,
        int runNo,
        string name,
        Guid actorId,
        DateTimeOffset now,
        Guid? testIssueProjectId = null,
        Guid? testIssueId = null)
    {
        Id = id;
        TestPlanId = planId;
        RunNo = runNo;
        Name = name;
        TestIssueProjectId = testIssueProjectId;
        TestIssueId = testIssueId;
        StartedByAccountId = actorId;
        Status = TestRunStatus.NotStarted;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestPlanId { get; private set; }
    public int RunNo { get; private set; }
    public string Name { get; private set; } = null!;
    public Guid? TestIssueProjectId { get; private set; }
    public Guid? TestIssueId { get; private set; }
    public TestRunStatus Status { get; private set; }
    public Guid StartedByAccountId { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Summary { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
    public ICollection<TestRunItem> Items { get; } = [];
    public TestPlan Plan { get; private set; } = null!;

    public void MarkInProgress(Guid actorId, DateTimeOffset now)
    {
        if (Status is not (TestRunStatus.NotStarted or TestRunStatus.Cancelled))
        {
            return;
        }

        Status = TestRunStatus.InProgress;
        CompletedAt = null;
        StartedAt = now;
        Touch(actorId, now);
    }

    public void Finish(TestRunStatus status, string? summary, Guid actorId, DateTimeOffset now)
    {
        Status = status;
        Summary = summary;
        StartedAt ??= now;
        CompletedAt = now;
        Touch(actorId, now);
    }

    private void Touch(Guid actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
        Version++;
    }
}
