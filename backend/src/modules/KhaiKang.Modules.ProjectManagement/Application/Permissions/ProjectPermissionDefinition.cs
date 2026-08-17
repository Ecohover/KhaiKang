namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record ProjectPermissionDefinition
{
    public ProjectPermissionDefinition(string id, string code)
    {
        Id = Guid.Parse(id);
        Code = code;
    }

    public Guid Id { get; }

    public string Code { get; }
}
