namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestWorkspace
{
    private TestWorkspace() { }

    public TestWorkspace(
        Guid id, string name, string prefix, string? description, Guid actorId, DateTimeOffset now)
    {
        Id = id;
        Name = name;
        Prefix = prefix;
        Description = description;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = actorId;
        UpdatedByAccountId = actorId;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Prefix { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Status { get; private set; } = "active";
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; } = 1;
    public ICollection<TestWorkspaceMember> Members { get; } = [];
    public ICollection<TestSuite> Suites { get; } = [];

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
