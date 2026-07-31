namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record UpdateProjectRequest(
    string Name,
    string? Description,
    string Status,
    int Version);
