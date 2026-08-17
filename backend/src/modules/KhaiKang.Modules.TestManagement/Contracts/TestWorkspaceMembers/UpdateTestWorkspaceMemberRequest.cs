namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record UpdateTestWorkspaceMemberRequest
{
    public UpdateTestWorkspaceMemberRequest(string role, int version)
    {
        Role = role;
        Version = version;
    }

    public string Role { get; }

    public int Version { get; }
}
