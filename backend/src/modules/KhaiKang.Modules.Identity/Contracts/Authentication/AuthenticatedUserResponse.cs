namespace KhaiKang.Modules.Identity.Contracts;

public sealed record AuthenticatedUserResponse
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }

    public required string AccountType { get; init; }

    public string? FixedRole { get; init; }

    public Guid? OwnerAccountId { get; init; }

    public required IReadOnlyList<string> SystemRoles { get; init; }

    public required IReadOnlyList<string> SystemPermissions { get; init; }

    public required bool MustChangePassword { get; init; }
}
