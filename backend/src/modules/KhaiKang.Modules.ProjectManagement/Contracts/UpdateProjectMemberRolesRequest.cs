namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record UpdateProjectMemberRolesRequest(
    IReadOnlyList<string> RoleCodes,
    int Version);
