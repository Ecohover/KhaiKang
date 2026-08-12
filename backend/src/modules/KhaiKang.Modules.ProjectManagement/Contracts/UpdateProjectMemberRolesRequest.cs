namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record UpdateProjectMemberRolesRequest
{
    public UpdateProjectMemberRolesRequest(
        IReadOnlyCollection<string> roleCodes,
        int version)
    {
        RoleCodes = roleCodes;
        Version = version;
    }

    public IReadOnlyCollection<string> RoleCodes { get; }

    public int Version { get; }
}
