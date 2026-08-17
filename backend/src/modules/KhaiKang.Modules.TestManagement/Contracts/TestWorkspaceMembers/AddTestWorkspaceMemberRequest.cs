namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record AddTestWorkspaceMemberRequest
{
    public AddTestWorkspaceMemberRequest(string username, string role)
    {
        Username = username;
        Role = role;
    }

    public string Username { get; }

    public string Role { get; }
}
