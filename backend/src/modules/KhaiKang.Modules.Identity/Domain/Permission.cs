namespace KhaiKang.Modules.Identity.Domain;

public sealed class Permission
{
    private Permission()
    {
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string ScopeType { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public int Version { get; private set; } = 1;

    public ICollection<SystemRolePermission> SystemRoles { get; } = [];
}
