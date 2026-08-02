namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectMemberRole
{
    private ProjectMemberRole()
    {
    }

    public ProjectMemberRole(
        Guid id,
        Guid projectMemberId,
        Guid projectRoleId,
        DateTimeOffset createdAt,
        Guid actorAccountId)
    {
        Id = id;
        ProjectMemberId = projectMemberId;
        ProjectRoleId = projectRoleId;
        CreatedAt = createdAt;
        CreatedByAccountId = actorAccountId;
        UpdatedAt = createdAt;
        UpdatedByAccountId = actorAccountId;
    }

    public Guid Id { get; private set; }

    public Guid ProjectMemberId { get; private set; }

    public ProjectMember ProjectMember { get; private set; } = null!;

    public Guid ProjectRoleId { get; private set; }

    public ProjectRole ProjectRole { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public int Version { get; private set; } = 1;
}
