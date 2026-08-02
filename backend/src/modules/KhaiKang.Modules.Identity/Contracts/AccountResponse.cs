namespace KhaiKang.Modules.Identity.Contracts;

public sealed record AccountResponse(
    Guid Id,
    string Username,
    string AccountType,
    string Status,
    IReadOnlyList<string> SystemRoles,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version);
