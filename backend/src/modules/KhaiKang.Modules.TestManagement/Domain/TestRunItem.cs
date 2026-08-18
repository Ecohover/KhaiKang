namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestRunItem
{
    private TestRunItem() { }

    public TestRunItem(Guid id, Guid runId, TestCase source, int sortOrder, Guid actorId, DateTimeOffset now)
    {
        Id = id;
        TestRunId = runId;
        TestCaseId = source.Id;
        SortOrder = sortOrder;
        CaseTitle = source.Title;
        CaseDescription = source.Description;
        Preconditions = source.Preconditions;
        OverallExpectedResult = source.OverallExpectedResult;
        ResultStatus = TestResultStatus.NotRun;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestRunId { get; private set; }
    public Guid TestCaseId { get; private set; }
    public int SortOrder { get; private set; }
    public string CaseTitle { get; private set; } = null!;
    public string? CaseDescription { get; private set; }
    public string? Preconditions { get; private set; }
    public string? OverallExpectedResult { get; private set; }
    public TestResultStatus ResultStatus { get; private set; }
    public string? ActualResult { get; private set; }
    public Guid? ExecutedByAccountId { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
    public ICollection<TestRunItemStepResult> Steps { get; } = [];

    public void Record(
        TestResultStatus status,
        string? actualResult,
        Guid actorId,
        DateTimeOffset now)
    {
        ResultStatus = status;
        ActualResult = actualResult;
        ExecutedByAccountId = status == TestResultStatus.NotRun ? null : actorId;
        ExecutedAt = status == TestResultStatus.NotRun ? null : now;
        UpdatedByAccountId = actorId;
        UpdatedAt = now;
        Version++;
    }
}
