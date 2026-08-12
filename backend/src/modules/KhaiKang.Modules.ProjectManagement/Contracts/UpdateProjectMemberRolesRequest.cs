namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record UpdateProjectMemberRolesRequest(
    IReadOnlyCollection<string> RoleCodes,
    int Version);
