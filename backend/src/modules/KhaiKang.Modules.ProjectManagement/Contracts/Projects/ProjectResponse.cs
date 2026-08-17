namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record ProjectResponse
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyList<string> CurrentUserRoles { get; init; }

    public required IReadOnlyList<string> CurrentUserPermissions { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required int Version { get; init; }
}
