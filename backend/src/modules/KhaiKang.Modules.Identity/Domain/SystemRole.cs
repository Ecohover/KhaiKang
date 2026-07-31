namespace KhaiKang.Modules.Identity.Domain;

public sealed class SystemRole
{
    private SystemRole()
    {
    }

    public SystemRole(Guid id, string name, string normalizedName)
    {
        Id = id;
        Name = name;
        NormalizedName = normalizedName;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public ICollection<AccountSystemRole> Accounts { get; } = [];

    public ICollection<SystemRolePermission> Permissions { get; } = [];
}
