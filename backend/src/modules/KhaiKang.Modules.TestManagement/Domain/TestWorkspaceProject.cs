namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestWorkspaceProject
{
    private TestWorkspaceProject()
    {
    }

    public TestWorkspaceProject(
        Guid id,
        Guid testWorkspaceId,
        Guid projectId,
        Guid actorId,
        DateTimeOffset now)
    {
        Id = id;
        TestWorkspaceId = testWorkspaceId;
        ProjectId = projectId;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
    }

    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public Guid ProjectId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; } = 1;
    public TestWorkspace Workspace { get; private set; } = null!;
}
