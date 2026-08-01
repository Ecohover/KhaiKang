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
        DateTimeOffset now)
    {
        Id = id; TestPlanId = planId; RunNo = runNo; Name = name;
        StartedByAccountId = actorId; Status = "not_started";
        CreatedAt = UpdatedAt = now; CreatedByAccountId = UpdatedByAccountId = actorId; Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestPlanId { get; private set; }
    public int RunNo { get; private set; }
    public string Name { get; private set; } = null!;
    public string Status { get; private set; } = null!;
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
        if (Status is not ("not_started" or "cancelled")) return;
        Status = "in_progress";
        CompletedAt = null;
        StartedAt = now;
        Touch(actorId, now);
    }

    public void Finish(string status, string? summary, Guid actorId, DateTimeOffset now)
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
