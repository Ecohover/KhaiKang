namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectMember
{
    private ProjectMember()
    {
    }

    public ProjectMember(
        Guid id,
        Guid projectId,
        Guid accountId,
        DateTimeOffset joinedAt,
        Guid actorAccountId)
    {
        Id = id;
        ProjectId = projectId;
        AccountId = accountId;
        JoinedAt = joinedAt;
        CreatedAt = joinedAt;
        CreatedByAccountId = actorAccountId;
        UpdatedAt = joinedAt;
        UpdatedByAccountId = actorAccountId;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public Guid AccountId { get; private set; }

    public string Status { get; private set; } = "active";

    public DateTimeOffset JoinedAt { get; private set; }

    public DateTimeOffset? RemovedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public int Version { get; private set; } = 1;

    public ICollection<ProjectMemberRole> Roles { get; } = [];

    public void Restore(Guid actorAccountId, DateTimeOffset occurredAt)
    {
        Status = "active";
        JoinedAt = occurredAt;
        RemovedAt = null;
        UpdatedAt = occurredAt;
        UpdatedByAccountId = actorAccountId;
        Version++;
    }

    public void Remove(Guid actorAccountId, DateTimeOffset occurredAt)
    {
        Status = "removed";
        RemovedAt = occurredAt;
        UpdatedAt = occurredAt;
        UpdatedByAccountId = actorAccountId;
        Version++;
    }

    public void RecordRoleChange(Guid actorAccountId, DateTimeOffset occurredAt)
    {
        UpdatedAt = occurredAt;
        UpdatedByAccountId = actorAccountId;
        Version++;
    }
}
