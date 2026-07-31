using KhaiKang.Modules.ProjectManagement.Infrastructure;

namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectRolePermission
{
    private ProjectRolePermission()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectRoleId { get; private set; }

    public ProjectRole ProjectRole { get; private set; } = null!;

    public Guid PermissionId { get; private set; }

    public PermissionReference Permission { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public int Version { get; private set; } = 1;
}
