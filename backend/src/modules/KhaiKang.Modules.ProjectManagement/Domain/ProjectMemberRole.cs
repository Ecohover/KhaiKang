namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectMemberRole : AuditableEntity
{
    private ProjectMemberRole()
    {
    }

    private ProjectMemberRole(ProjectMemberRoleCreation creation, ChangeContext context)
    {
        Id = creation.Id;
        ProjectMemberId = creation.ProjectMemberId;
        ProjectRoleId = creation.ProjectRoleId;
        InitializeAudit(context);
    }

    public static ProjectMemberRole Create(
        ProjectMemberRoleCreation creation,
        ChangeContext context)
    {
        return new ProjectMemberRole(creation, context);
    }

    public Guid Id { get; private set; }

    public Guid ProjectMemberId { get; private set; }

    public ProjectMember ProjectMember { get; private set; } = null!;

    public Guid ProjectRoleId { get; private set; }

    public ProjectRole ProjectRole { get; private set; } = null!;

}
