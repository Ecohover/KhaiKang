namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestCase
{
    private TestCase() { }

    public TestCase(
        Guid id,
        Guid suiteId,
        string title,
        string? description,
        string? preconditions,
        string? overallExpectedResult,
        int sortOrder,
        Guid actorId,
        DateTimeOffset now)
    {
        Id = id;
        TestSuiteId = suiteId;
        Title = title;
        Description = description;
        Preconditions = preconditions;
        OverallExpectedResult = overallExpectedResult;
        SortOrder = sortOrder;
        Status = "active";
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestSuiteId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Preconditions { get; private set; }
    public string? OverallExpectedResult { get; private set; }
    public int SortOrder { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
    public TestSuite Suite { get; private set; } = null!;
    public ICollection<TestStep> Steps { get; } = [];

    public void AddStep(TestStep step)
    {
        Steps.Add(step);
    }

    public void ClearSteps()
    {
        Steps.Clear();
    }

    public void Update(
        Guid suiteId,
        string title,
        string? description,
        string? preconditions,
        string? overallExpectedResult,
        int sortOrder,
        string status,
        Guid actorId,
        DateTimeOffset now)
    {
        TestSuiteId = suiteId;
        Title = title;
        Description = description;
        Preconditions = preconditions;
        OverallExpectedResult = overallExpectedResult;
        SortOrder = sortOrder;
        Status = status;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
        Version++;
    }
}
