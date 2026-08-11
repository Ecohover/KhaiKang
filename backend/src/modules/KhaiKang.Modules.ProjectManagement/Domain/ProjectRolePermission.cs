using KhaiKang.Modules.ProjectManagement.Infrastructure;

namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectRolePermission : AuditableEntity
{
    private ProjectRolePermission()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectRoleId { get; private set; }

    public ProjectRole ProjectRole { get; private set; } = null!;

    public Guid PermissionId { get; private set; }

    public PermissionReference Permission { get; private set; } = null!;

}
