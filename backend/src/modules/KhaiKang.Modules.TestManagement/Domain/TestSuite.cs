namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestSuite
{
    private TestSuite() { }

    public TestSuite(
        Guid id,
        Guid workspaceId,
        Guid? parentId,
        string name,
        string? description,
        int sortOrder,
        Guid actorId,
        DateTimeOffset now)
    {
        Id = id;
        TestWorkspaceId = workspaceId;
        ParentId = parentId;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
    }

    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public TestAssetStatus Status { get; private set; } = TestAssetStatus.Active;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; } = 1;
    public TestWorkspace Workspace { get; private set; } = null!;

    public void Update(
        Guid? parentId,
        string name,
        string? description,
        int sortOrder,
        TestAssetStatus status,
        Guid actorId,
        DateTimeOffset now)
    {
        ParentId = parentId;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        Status = status;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
        Version++;
    }
}
