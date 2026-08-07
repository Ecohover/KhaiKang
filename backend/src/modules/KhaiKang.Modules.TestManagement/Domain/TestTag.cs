namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestTag
{
    private TestTag() { }
    public TestTag(Guid id, string name, string? description, Guid actorId, DateTimeOffset now)
    {
        Id = id;
        Name = name;
        Description = description;
        Status = "active";
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
    public ICollection<TestCaseTag> Cases { get; } = [];

    public void Update(string name, string? description, string status, Guid actorId, DateTimeOffset now)
    {
        Name = name;
        Description = description;
        Status = status;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
        Version++;
    }
}
