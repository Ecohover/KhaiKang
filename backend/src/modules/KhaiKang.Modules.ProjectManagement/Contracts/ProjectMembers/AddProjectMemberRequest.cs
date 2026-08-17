namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record AddProjectMemberRequest
{
    public AddProjectMemberRequest(
        string username,
        IReadOnlyCollection<string> roleCodes)
    {
        Username = username;
        RoleCodes = roleCodes;
    }

    public string Username { get; }

    public IReadOnlyCollection<string> RoleCodes { get; }
}
