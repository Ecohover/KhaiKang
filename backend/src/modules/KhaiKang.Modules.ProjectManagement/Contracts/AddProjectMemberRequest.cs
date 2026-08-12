namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record AddProjectMemberRequest(
    string Username,
    IReadOnlyCollection<string> RoleCodes);
