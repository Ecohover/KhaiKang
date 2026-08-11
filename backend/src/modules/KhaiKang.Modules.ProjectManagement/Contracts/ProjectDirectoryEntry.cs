using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record ProjectDirectoryEntry(
    Guid Id,
    string Code,
    string Name,
    ProjectStatus Status);
