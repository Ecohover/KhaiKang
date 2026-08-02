namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestTag
{
    private TestTag() { }
    public TestTag(Guid id, Guid workspaceId, string name)
    { Id = id; TestWorkspaceId = workspaceId; Name = name; }
    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public ICollection<TestCaseTag> Cases { get; } = [];
}
