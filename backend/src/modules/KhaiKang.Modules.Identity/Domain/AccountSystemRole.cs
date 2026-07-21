namespace KhaiKang.Modules.Identity.Domain;

public sealed class AccountSystemRole
{
    private AccountSystemRole()
    {
    }

    public AccountSystemRole(Guid accountId, Guid systemRoleId)
    {
        AccountId = accountId;
        SystemRoleId = systemRoleId;
    }

    public Guid AccountId { get; private set; }

    public Account Account { get; private set; } = null!;

    public Guid SystemRoleId { get; private set; }

    public SystemRole SystemRole { get; private set; } = null!;
}
