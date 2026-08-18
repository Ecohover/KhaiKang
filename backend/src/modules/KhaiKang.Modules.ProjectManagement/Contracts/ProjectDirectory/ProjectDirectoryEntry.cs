using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record ProjectDirectoryEntry
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required ProjectStatus Status { get; init; }
}
