namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectMember : AuditableEntity
{
    private ProjectMember()
    {
    }

    private ProjectMember(ProjectMemberCreation creation, ChangeContext context)
    {
        Id = creation.Id;
        ProjectId = creation.ProjectId;
        AccountId = creation.AccountId;
        JoinedAt = context.OccurredAt;
        InitializeAudit(context);
    }

    public static ProjectMember Create(ProjectMemberCreation creation, ChangeContext context)
    {
        return new ProjectMember(creation, context);
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public Guid AccountId { get; private set; }

    public ProjectMemberStatus Status { get; private set; } = ProjectMemberStatus.Active;

    public DateTimeOffset JoinedAt { get; private set; }

    public DateTimeOffset? RemovedAt { get; private set; }

    public ICollection<ProjectMemberRole> Roles { get; } = [];

    public void Restore(ChangeContext context)
    {
        Status = ProjectMemberStatus.Active;
        JoinedAt = context.OccurredAt;
        RemovedAt = null;
        RecordChange(context);
    }

    public void Remove(ChangeContext context)
    {
        Status = ProjectMemberStatus.Removed;
        RemovedAt = context.OccurredAt;
        RecordChange(context);
    }

    public void RecordRoleChange(ChangeContext context)
    {
        RecordChange(context);
    }
}
