namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectRole : AuditableEntity
{
    private ProjectRole()
    {
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public bool IsSystem { get; private set; } = true;

    public bool IsActive { get; private set; } = true;

    public int SortOrder { get; private set; }

    public ICollection<ProjectRolePermission> Permissions { get; } = [];
}
