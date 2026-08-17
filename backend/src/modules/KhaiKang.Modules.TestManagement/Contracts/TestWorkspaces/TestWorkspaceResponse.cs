namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestWorkspaceResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Prefix { get; init; }

    public required string? Description { get; init; }

    public required string Status { get; init; }

    public required string CurrentUserRole { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required int Version { get; init; }
}
