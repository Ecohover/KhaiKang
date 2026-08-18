using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueDirectoryEntry
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string ProjectCode { get; init; }

    public required ProjectStatus ProjectStatus { get; init; }

    public required int IssueNo { get; init; }

    public required string Key { get; init; }

    public required string Title { get; init; }

    public required string TypeCode { get; init; }

    public required string StatusCode { get; init; }
}
