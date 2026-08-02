namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestRunItemStepResult
{
    private TestRunItemStepResult() { }

    public TestRunItemStepResult(Guid id, Guid runItemId, TestStep source, Guid actorId, DateTimeOffset now)
    {
        Id = id;
        TestRunItemId = runItemId;
        StepNo = source.StepNo;
        Action = source.Action;
        ExpectedResult = source.ExpectedResult;
        ResultStatus = "not_run";
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestRunItemId { get; private set; }
    public int StepNo { get; private set; }
    public string Action { get; private set; } = null!;
    public string ExpectedResult { get; private set; } = null!;
    public string ResultStatus { get; private set; } = null!;
    public string? ActualResult { get; private set; }
    public Guid? ExecutedByAccountId { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }

    public void Record(string status, string? actualResult, Guid actorId, DateTimeOffset now)
    {
        ResultStatus = status;
        ActualResult = actualResult;
        ExecutedByAccountId = status == "not_run" ? null : actorId;
        ExecutedAt = status == "not_run" ? null : now;
        UpdatedByAccountId = actorId;
        UpdatedAt = now;
        Version++;
    }
}
