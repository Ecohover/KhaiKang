namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class ProjectRole
{
    private ProjectRole()
    {
    }

    public ProjectRole(
        Guid id,
        string code,
        string name,
        string description,
        int sortOrder,
        DateTimeOffset createdAt)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public bool IsSystem { get; private set; } = true;

    public bool IsActive { get; private set; } = true;

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public int Version { get; private set; } = 1;

    public ICollection<ProjectRolePermission> Permissions { get; } = [];
}
