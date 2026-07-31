namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestCaseTag
{
    private TestCaseTag() { }
    public TestCaseTag(Guid testCaseId, Guid testTagId)
    {
        TestCaseId = testCaseId;
        TestTagId = testTagId;
    }
    public Guid TestCaseId { get; private set; }
    public Guid TestTagId { get; private set; }
    public TestCase TestCase { get; private set; } = null!;
    public TestTag Tag { get; private set; } = null!;
}
