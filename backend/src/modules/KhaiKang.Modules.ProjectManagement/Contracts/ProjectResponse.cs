namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record ProjectResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Status,
    IReadOnlyList<string> CurrentUserRoles,
    IReadOnlyList<string> CurrentUserPermissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version);
