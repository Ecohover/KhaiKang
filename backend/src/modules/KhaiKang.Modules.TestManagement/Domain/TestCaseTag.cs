namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestCaseTag
{
    private TestCaseTag() { }
    public TestCaseTag(Guid id, Guid testCaseId, Guid testTagId, Guid actorId, DateTimeOffset now)
    {
        Id = id;
        TestCaseId = testCaseId;
        TestTagId = testTagId;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }
    public Guid Id { get; private set; }
    public Guid TestCaseId { get; private set; }
    public Guid TestTagId { get; private set; }
    public TestCase TestCase { get; private set; } = null!;
    public TestTag Tag { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
}
