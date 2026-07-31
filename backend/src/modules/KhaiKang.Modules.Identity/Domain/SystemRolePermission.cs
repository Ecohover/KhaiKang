namespace KhaiKang.Modules.Identity.Domain;

public sealed class SystemRolePermission
{
    private SystemRolePermission()
    {
    }

    public SystemRolePermission(
        Guid id,
        Guid systemRoleId,
        Guid permissionId,
        DateTimeOffset createdAt,
        Guid? actorAccountId)
    {
        Id = id;
        SystemRoleId = systemRoleId;
        PermissionId = permissionId;
        CreatedAt = createdAt;
        CreatedByAccountId = actorAccountId;
        UpdatedAt = createdAt;
        UpdatedByAccountId = actorAccountId;
    }

    public Guid Id { get; private set; }

    public Guid SystemRoleId { get; private set; }

    public SystemRole SystemRole { get; private set; } = null!;

    public Guid PermissionId { get; private set; }

    public Permission Permission { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public int Version { get; private set; } = 1;
}
