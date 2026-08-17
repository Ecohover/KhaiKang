namespace KhaiKang.Modules.Identity.Contracts;

public sealed record AccountResponse
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }

    public required string AccountType { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyList<string> SystemRoles { get; init; }

    public required bool MustChangePassword { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required int Version { get; init; }
}
