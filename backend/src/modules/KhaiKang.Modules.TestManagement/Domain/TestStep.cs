namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestStep
{
    private TestStep() { }

    public TestStep(
        Guid id,
        Guid testCaseId,
        int stepNo,
        string action,
        string expectedResult,
        Guid actorId,
        DateTimeOffset now)
    {
        Id = id;
        TestCaseId = testCaseId;
        StepNo = stepNo;
        Action = action;
        ExpectedResult = expectedResult;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestCaseId { get; private set; }
    public int StepNo { get; private set; }
    public string Action { get; private set; } = null!;
    public string ExpectedResult { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
    public TestCase TestCase { get; private set; } = null!;
}
