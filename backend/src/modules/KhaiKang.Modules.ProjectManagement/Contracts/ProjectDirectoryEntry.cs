namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record ProjectDirectoryEntry(
    Guid Id,
    string Code,
    string Name,
    string Status);
