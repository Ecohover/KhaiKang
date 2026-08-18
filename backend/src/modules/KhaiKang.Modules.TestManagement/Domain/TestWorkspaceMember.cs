namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestWorkspaceMember
{
    private TestWorkspaceMember() { }

    public TestWorkspaceMember(
        Guid id,
        Guid workspaceId,
        Guid accountId,
        TestWorkspaceRole role,
        Guid actorId,
        DateTimeOffset now)
    {
        Id = id;
        TestWorkspaceId = workspaceId;
        AccountId = accountId;
        Role = role;
        JoinedAt = now;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
    }

    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public Guid AccountId { get; private set; }
    public TestWorkspaceRole Role { get; private set; }
    public TestWorkspaceMemberStatus Status { get; private set; } = TestWorkspaceMemberStatus.Active;
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; } = 1;
    public TestWorkspace Workspace { get; private set; } = null!;

    public void ChangeRole(TestWorkspaceRole role, Guid actorId, DateTimeOffset now)
    {
        Role = role;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
        Version++;
    }

    public void Remove(Guid actorId, DateTimeOffset now)
    {
        Status = TestWorkspaceMemberStatus.Removed;
        RemovedAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
        Version++;
    }

    public void Restore(TestWorkspaceRole role, Guid actorId, DateTimeOffset now)
    {
        Role = role;
        Status = TestWorkspaceMemberStatus.Active;
        RemovedAt = null;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
        Version++;
    }
}
