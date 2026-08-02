namespace KhaiKang.Modules.ProjectManagement.Infrastructure;

public sealed class PermissionReference
{
    private PermissionReference()
    {
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;
}
